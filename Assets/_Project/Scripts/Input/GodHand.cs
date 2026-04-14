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
        public float longPressDuration = 0.5f;
        public float pickupHeight = 2.5f;
        public float smoothSpeed = 12f;
        public LayerMask draggableLayer;

        [Header("Fling")]
        public float flingVelocityThreshold = 500f;
        public float flingForceMultiplier = 1.5f;

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
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                
                _isPressing = true;
                _pressTimer = 0f;
                _pressStartPos = screenPos;
            }

            if (_isPressing && held)
            {
                if (!_isDragging)
                {
                    _pressTimer += Time.deltaTime;
                    if (Vector2.Distance(screenPos, _pressStartPos) > 50f) // Soglia aumentata da 20 a 50
                    {
                        _isPressing = false;
                    }
                    else if (_pressTimer >= longPressDuration)
                    {
                        Debug.Log("[GodHand] Long Press rilevato, tento Grab...");
                        TryGrab(screenPos);
                    }
                }
            }

            if (released)
            {
                if (_isDragging) Release();
                _isPressing = false;
                _isDragging = false;
            }
        }

        private void TryGrab(Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            // SphereCast con raggio di 0.5m per essere meno "pignoli" nel puntamento
            RaycastHit[] hits = Physics.SphereCastAll(ray, 0.5f, 200f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundTarget = false;
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == 5) continue; // Salta UI

                var villager = hit.collider.GetComponentInParent<VillagerController>();
                if (villager != null && villager.CurrentState != VillagerController.VillagerState.Dead)
                {
                    Debug.Log($"[GodHand] Grab SphereCast: {villager.name} a distanza {hit.distance}");
                    Grab(villager.gameObject);
                    foundTarget = true;
                    break;
                }

                var dmgObj = hit.collider.GetComponentInParent<DamageableObject>();
                if (dmgObj != null)
                {
                    Debug.Log($"[GodHand] Grab SphereCast: {dmgObj.name} a distanza {hit.distance}");
                    Grab(dmgObj.gameObject);
                    foundTarget = true;
                    break;
                }
            }

            if (!foundTarget) Debug.Log($"[GodHand] Grab fallito su {screenPos}. Nessun target trovato con SphereCast.");
            _isPressing = false;
        }

        private void Grab(GameObject target)
        {
            _heldObject = target;
            _isDragging = true;
            _lastHeldPos = _heldObject.transform.position;
            _dragVelocity = Vector3.zero;
            
            _heldAgent = _heldObject.GetComponent<NavMeshAgent>();
            if (_heldAgent != null) _heldAgent.enabled = false;

            _heldRb = _heldObject.GetComponent<Rigidbody>();
            if (_heldRb != null)
            {
                _heldRb.isKinematic = true;
                _heldRb.useGravity = false;
            }

            // VFX/Feedback
            if (grabVFXPrefab != null)
            {
                Instantiate(grabVFXPrefab, _heldObject.transform.position, Quaternion.identity, _heldObject.transform);
            }

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("UP!", _heldObject.transform.position + Vector3.up * 2f, Color.cyan);
        }

        private void UpdateHeldObjectPosition()
        {
            Vector2 screenPos = GetInputScreenPos();
            Ray ray = _camera.ScreenPointToRay(screenPos);
            
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                if (enter > 0)
                {
                    Vector3 worldPos = ray.GetPoint(enter);
                    Vector3 targetPos = worldPos + Vector3.up * pickupHeight;

                    // Calcola velocità di trascinamento basata sul movimento reale del tocco (prima del Lerp)
                    if (Time.deltaTime > 0)
                    {
                        _dragVelocity = (targetPos - _lastHeldPos) / Time.deltaTime;
                        _lastHeldPos = targetPos;
                    }

                    _heldObject.transform.position = Vector3.Lerp(_heldObject.transform.position, targetPos, Time.deltaTime * smoothSpeed);
                    
                    // Rotazione "pendolo" leggera o semplicemente guarda avanti
                    _heldObject.transform.rotation = Quaternion.Slerp(_heldObject.transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
                }
            }
        }

        private void Release()
        {
            if (_heldObject == null) return;

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
                {
                    dropPos = hit.position;
                }
                else
                {
                    dropPos.y = 0f;
                }

                _heldObject.transform.position = dropPos;

                if (_heldAgent != null) _heldAgent.enabled = true;
                if (_heldRb != null)
                {
                    _heldRb.isKinematic = true; 
                }

                if (villager != null)
                {
                    villager.GoIdleDirect();
                    if (FloatingTextSpawner.Instance != null)
                        FloatingTextSpawner.Instance.Spawn("Dropped", dropPos + Vector3.up * 2f, Color.white);
                }
            }

            _heldObject = null;
            _heldAgent = null;
            _heldRb = null;
            _dragVelocity = Vector3.zero;
        }

        // ── Input Abstraction ──────────────────────────────────────────

        private bool GetInputDown(out Vector2 pos)
        {
            // Priorità Mouse su desktop
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { 
                pos = Mouse.current.position.ReadValue(); 
                if (pos.sqrMagnitude > 0.01f) {
                    Debug.Log($"[GodHand] Mouse Down: {pos}");
                    return true;
                }
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { 
                pos = Touchscreen.current.primaryTouch.position.ReadValue(); 
                if (pos.sqrMagnitude > 0.01f) {
                    Debug.Log($"[GodHand] Touch Down: {pos}");
                    return true; 
                }
            }
            pos = Vector2.zero; return false;
        }

        private bool GetInputHeld(out Vector2 pos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { 
                pos = Mouse.current.position.ReadValue(); 
                if (pos.sqrMagnitude > 0.01f) return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { 
                pos = Touchscreen.current.primaryTouch.position.ReadValue(); 
                if (pos.sqrMagnitude > 0.01f) return true; 
            }
            pos = Vector2.zero; return false;
        }

        private bool GetInputUp(out Vector2 pos)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            { 
                pos = Mouse.current.position.ReadValue(); return true; 
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            { 
                pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; 
            }
            pos = Vector2.zero; return false;
        }

        private Vector2 GetInputScreenPos()
        {
            Vector2 pos = Vector2.zero;

            // 1. Prova con Pointer (Astrazione per Mouse/Touch)
            if (Pointer.current != null) {
                pos = Pointer.current.position.ReadValue();
                if (pos.sqrMagnitude > 0.01f) return pos;
            }

            // 2. Prova con Mouse esplicito
            if (Mouse.current != null) {
                pos = Mouse.current.position.ReadValue();
                if (pos.sqrMagnitude > 0.01f) return pos;
            }

            // 3. Prova con Touch esplicito
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (pos.sqrMagnitude > 0.01f) return pos;
            }

            // 4. Fallback estremo: Legacy Input (funziona spesso anche con New System)
            pos = new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y);
            
            return pos;
        }
    }
}
