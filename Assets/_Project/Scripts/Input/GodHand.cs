using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DivinePrototype
{
    /// <summary>
    /// Gestisce il Drag & Drop fisico di Villager e Oggetti.
    /// Pressione prolungata -> solleva l'oggetto. Drag -> lo sposta. Rilascio -> lo posa.
    /// </summary>
    public class GodHand : MonoBehaviour
    {
        [Header("Configurazione")]
        public float longPressDuration = 0.4f; 
        public float pickupHeight = 2.8f;
        public float smoothSpeed = 15f;
        public LayerMask draggableLayer = ~0; 

        [Header("Tolleranze")]
        public float moveTolerancePixels = 80f; 

        [Header("Fling")]
        public float flingVelocityThreshold = 400f;
        public float flingForceMultiplier = 1.3f;

        [Header("Feedback")]
        public GameObject grabVFXPrefab;

        private Camera _camera;
        private GameObject _heldObject;
        private NavMeshAgent _heldAgent;
        private Rigidbody _heldRb;
        
        private float _pressTimer = 0f;
        private bool _isPressing = false;
        private bool _isDragging = false;
        private Vector2 _pressStartPos;
        private Vector3 _lastHeldPos;
        private Vector3 _dragVelocity;

        private void Awake()
        {
            _camera = Camera.main;
            enabled = true; 
            Debug.Log("[GodHand] Awake: Componente forzato a ENABLED.");
        }

        private void OnEnable()
        {
            Debug.Log("[GodHand] OnEnable: Il sistema è ora attivo.");
        }

        private void Start()
        {
            Debug.Log("[GodHand] Start: Sistema pronto al primo frame.");
        }

        private void Update()
        {
            HandleInput();
            if (_isDragging && _heldObject != null)
            {
                UpdateHeldObjectPosition();
            }
        }

        private void HandleInput()
        {
            Vector2 screenPos;
            bool pressed = GetInputDown(out screenPos);
            bool held = GetInputHeld(out screenPos);
            bool released = GetInputUp(out screenPos);

            if (pressed)
            {
                // Se la posizione è (0,0), proviamo a recuperarla forzatamente
                if (screenPos.sqrMagnitude < 0.001f) screenPos = GetInputScreenPos();

                Debug.Log($"[GodHand] Input Down rilevato a {screenPos}.");

                if (screenPos.sqrMagnitude > 0.001f && IsPointerOverUI(screenPos)) {
                    Debug.Log("[GodHand] Click su UI rilevato. Ignoro grab.");
                    return;
                }
                
                _isPressing = true;
                _pressTimer = 0f;
                _pressStartPos = screenPos;
            }

            if (_isPressing && held)
            {
                if (!_isDragging)
                {
                    _pressTimer += Time.deltaTime;
                    
                    Vector2 currentPos = GetInputScreenPos();
                    float dist = Vector2.Distance(currentPos, _pressStartPos);

                    if (currentPos.sqrMagnitude > 0.001f && dist > moveTolerancePixels)
                    {
                        Debug.Log($"[GodHand] Long Press annullato per movimento ({dist:F0}px)");
                        _isPressing = false;
                    }
                    else if (_pressTimer >= longPressDuration)
                    {
                        Debug.Log($"[GodHand] Long Press OK ({_pressTimer:F2}s). Tento grab a {currentPos}...");
                        TryGrab(currentPos);
                    }
                }
            }

            if (released)
            {
                if (_isDragging) Release();
                else if (_isPressing) Debug.Log("[GodHand] Rilasciato troppo presto.");
                
                _isPressing = false;
                _isDragging = false;
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = screenPos;
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            if (results.Count > 0)
            {
                Debug.Log($"[GodHand] UI rilevata: {results[0].gameObject.name} su canvas {results[0].gameObject.GetComponentInParent<Canvas>()?.name}");
                // Bypassiamo il blocco per ora per farti testare il grab
                // return true; 
            }
            return false; 
        }

        private void TryGrab(Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            
            // Usiamo un raggio SphereCast generoso (1.2 unità)
            RaycastHit[] hits = Physics.SphereCastAll(ray, 1.2f, 100f, draggableLayer);
            Debug.Log($"[GodHand] SphereCast trovati: {hits.Length} collisori.");
            
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                GameObject go = hit.collider.gameObject;
                if (go.layer == 5) continue; // Layer UI

                Debug.Log($"[GodHand] Analizzo: {go.name} (Layer: {go.layer})");

                var villager = go.GetComponentInParent<VillagerController>();
                if (villager != null)
                {
                    if (villager.CurrentState == VillagerController.VillagerState.Dead) continue;
                    Grab(villager.gameObject);
                    return;
                }

                var dmgObj = go.GetComponentInParent<DamageableObject>();
                if (dmgObj != null)
                {
                    Grab(dmgObj.gameObject);
                    return;
                }
            }

            Debug.Log("[GodHand] Nessun target valido (Villager/Oggetto) trovato.");
            _isPressing = false;
        }

        private void Grab(GameObject target)
        {
            Debug.Log($"[GodHand] >>> GRAB! {target.name} <<<");
            _heldObject = target;
            _isDragging = true;
            _isPressing = false;
            _lastHeldPos = _heldObject.transform.position;
            
            _heldAgent = _heldObject.GetComponent<NavMeshAgent>();
            if (_heldAgent != null) _heldAgent.enabled = false;

            _heldRb = _heldObject.GetComponent<Rigidbody>();
            if (_heldRb != null)
            {
                _heldRb.isKinematic = true;
                _heldRb.useGravity = false;
            }

            if (grabVFXPrefab != null)
            {
                Instantiate(grabVFXPrefab, _heldObject.transform.position, Quaternion.identity, _heldObject.transform);
            }

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("GRAB!", _heldObject.transform.position + Vector3.up * 2f, Color.cyan);
        }

        private void UpdateHeldObjectPosition()
        {
            Vector2 screenPos = GetInputScreenPos();
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0, pickupHeight, 0));
            
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                if (Time.deltaTime > 0)
                {
                    _dragVelocity = (worldPos - _lastHeldPos) / Time.deltaTime;
                    _lastHeldPos = worldPos;
                }

                _heldObject.transform.position = Vector3.Lerp(_heldObject.transform.position, worldPos, Time.deltaTime * smoothSpeed);
                _heldObject.transform.rotation = Quaternion.Slerp(_heldObject.transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
            }
        }

        private void Release()
        {
            if (_heldObject == null) return;
            Debug.Log($"[GodHand] Release: {_heldObject.name}");

            var villager = _heldObject.GetComponent<VillagerController>();
            bool isFling = _dragVelocity.magnitude > flingVelocityThreshold;

            if (isFling && villager != null)
            {
                if (_heldRb != null)
                {
                    _heldRb.isKinematic = false;
                    _heldRb.useGravity = true;
                    _heldRb.AddForce(_dragVelocity * flingForceMultiplier, ForceMode.Impulse);
                }
                villager.SetSocialState(VillagerController.VillagerState.DivineProjectile);
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("YEET!", _heldObject.transform.position + Vector3.up * 2f, Color.magenta);
            }
            else
            {
                Vector3 dropPos = _heldObject.transform.position;
                if (NavMesh.SamplePosition(dropPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    dropPos = hit.position;

                _heldObject.transform.position = dropPos;
                if (_heldAgent != null) _heldAgent.enabled = true;
                if (_heldRb != null) _heldRb.isKinematic = true;
                if (villager != null) villager.GoIdleDirect();
                
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("Dropped", dropPos + Vector3.up * 2f, Color.white);
            }

            _heldObject = null;
            _heldAgent = null;
            _heldRb = null;
            _isDragging = false;
        }

        private bool GetInputDown(out Vector2 pos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { pos = Mouse.current.position.ReadValue(); return true; }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private bool GetInputHeld(out Vector2 pos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { pos = Mouse.current.position.ReadValue(); return true; }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private bool GetInputUp(out Vector2 pos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            { pos = Mouse.current.position.ReadValue(); return true; }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private Vector2 GetInputScreenPos()
        {
            if (Pointer.current != null) return Pointer.current.position.ReadValue();
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }
    }
}
