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
                    if (Vector2.Distance(screenPos, _pressStartPos) > 20f) // Mosso troppo, annulla long press
                    {
                        _isPressing = false;
                    }
                    else if (_pressTimer >= longPressDuration)
                    {
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
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, draggableLayer))
            {
                // Cerca VillagerController o DamageableObject
                var villager = hit.collider.GetComponentInParent<VillagerController>();
                var dmgObj = hit.collider.GetComponentInParent<DamageableObject>();

                if (villager != null)
                {
                    if (villager.CurrentState == VillagerController.VillagerState.Dead) return;
                    Grab(villager.gameObject);
                }
                else if (dmgObj != null)
                {
                    Grab(dmgObj.gameObject);
                }
            }
            _isPressing = false;
        }

        private void Grab(GameObject target)
        {
            _heldObject = target;
            _isDragging = true;
            
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
                Vector3 worldPos = ray.GetPoint(enter);
                Vector3 targetPos = worldPos + Vector3.up * pickupHeight;
                
                _heldObject.transform.position = Vector3.Lerp(_heldObject.transform.position, targetPos, Time.deltaTime * smoothSpeed);
                
                // Rotazione "pendolo" leggera o semplicemente guarda avanti
                _heldObject.transform.rotation = Quaternion.Slerp(_heldObject.transform.rotation, Quaternion.identity, Time.deltaTime * 5f);
            }
        }

        private void Release()
        {
            if (_heldObject == null) return;

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
                _heldRb.isKinematic = true; // In questo gioco preferiamo cinematic
            }

            var villager = _heldObject.GetComponent<VillagerController>();
            if (villager != null)
            {
                villager.GoIdleDirect();
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("Dropped", dropPos + Vector3.up * 2f, Color.white);
            }

            _heldObject = null;
            _heldAgent = null;
            _heldRb = null;
        }

        // ── Input Abstraction ──────────────────────────────────────────

        private bool GetInputDown(out Vector2 pos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { pos = Mouse.current.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private bool GetInputHeld(out Vector2 pos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { pos = Mouse.current.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private bool GetInputUp(out Vector2 pos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            { pos = Mouse.current.position.ReadValue(); return true; }
            pos = Vector2.zero; return false;
        }

        private Vector2 GetInputScreenPos()
        {
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }
    }
}
