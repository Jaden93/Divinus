using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DivinePrototype
{
    public class GodHand : MonoBehaviour
    {
        [Header("Configurazione")]
        public float longPressDuration = 0.3f; 
        public float pickupHeight = 3.5f;
        public float smoothSpeed = 25f;
        public LayerMask draggableLayer = ~0; 

        [Header("Tolleranze")]
        public float moveTolerancePixels = 150f; 

        [Header("Fling (Lancio)")]
        [Tooltip("Soglia di velocità per attivare il lancio")]
        public float flingThresholdForced = 2.0f;
        public float flingForceMultiplier = 2.5f; 
        public float upwardForceBoost = 0.5f; 
        public float ragdollThreshold = 10.0f;
        public float maxFlingForce = 100f; 

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
        
        private Vector3 _currentVelocity;
        private struct PositionSample {
            public Vector3 position;
            public float time;
        }
        private Queue<PositionSample> _samples = new Queue<PositionSample>();
        private const float SampleDuration = 0.1f; 

        private void Awake() 
        { 
            _camera = Camera.main; 
            enabled = true; 
            // Forza di nuovo per sicurezza
            flingThresholdForced = 1.0f;
            Debug.Log("[GodHand] Awake: Pronto. Soglia Lancio Forzata a: " + flingThresholdForced);
        }

        private void Update()
        {
            if (_isDragging && _heldObject != null) UpdateHeldObjectPosition();
            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 screenPos;
            bool pressed = GetInputDown(out screenPos);
            bool held = GetInputHeld(out screenPos);
            bool released = GetInputUp(out screenPos);

            if (pressed)
            {
                if (screenPos.sqrMagnitude < 0.001f) screenPos = GetInputScreenPos();
                _isPressing = true; _pressTimer = 0f; _pressStartPos = screenPos;
            }

            if (_isPressing && held && !_isDragging)
            {
                _pressTimer += Time.deltaTime;
                Vector2 currentPos = GetInputScreenPos();
                float dist = Vector2.Distance(currentPos, _pressStartPos);
                if (dist > moveTolerancePixels) _isPressing = false;
                else if (_pressTimer >= longPressDuration) TryGrab(currentPos);
            }

            if (released)
            {
                if (_isDragging) Release();
                _isPressing = false; _isDragging = false;
            }
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var res in results) if (res.gameObject.GetComponentInParent<VillagerController>() == null) return true;
            return false; 
        }

        private void TryGrab(Vector2 screenPos)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.SphereCastAll(ray, 1.2f, 100f, draggableLayer);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits) {
                GameObject go = hit.collider.gameObject;
                if (go.layer == 5) continue; 
                var villager = go.GetComponentInParent<VillagerController>();
                if (villager != null && villager.CurrentState != VillagerController.VillagerState.Dead) {
                    Grab(villager.gameObject); return;
                }
                var dmgObj = go.GetComponentInParent<DamageableObject>();
                if (dmgObj != null) { Grab(dmgObj.gameObject); return; }
            }
            _isPressing = false;
        }

        private void Grab(GameObject target)
        {
            _heldObject = target;
            _isDragging = true;
            _isPressing = false;
            _samples.Clear();
            _currentVelocity = Vector3.zero;

            _heldAgent = _heldObject.GetComponent<NavMeshAgent>();
            if (_heldAgent != null) { _heldAgent.isStopped = true; _heldAgent.enabled = false; }

            _heldRb = _heldObject.GetComponent<Rigidbody>();
            if (_heldRb != null) { _heldRb.isKinematic = true; _heldRb.useGravity = false; _heldRb.linearVelocity = Vector3.zero; }

            if (FloatingTextSpawner.Instance != null) FloatingTextSpawner.Instance.Spawn("GRAB!", _heldObject.transform.position + Vector3.up * 2f, Color.cyan);
            Debug.Log($"[GodHand] >>> GRAB! {_heldObject.name} <<<");
        }

        private void UpdateHeldObjectPosition()
        {
            Vector2 screenPos = GetInputScreenPos();
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane dragPlane = new Plane(Vector3.up, new Vector3(0, pickupHeight, 0));
            
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                
                // Campionamento preciso della velocità (ultimi 100ms)
                _samples.Enqueue(new PositionSample { position = worldPos, time = Time.unscaledTime });
                while (_samples.Count > 1 && Time.unscaledTime - _samples.Peek().time > SampleDuration)
                {
                    _samples.Dequeue();
                }

                if (_samples.Count > 1)
                {
                    var first = _samples.Peek();
                    float dt = Time.unscaledTime - first.time;
                    if (dt > 0) _currentVelocity = (worldPos - first.position) / dt;
                }

                _heldObject.transform.position = Vector3.Lerp(_heldObject.transform.position, worldPos, Time.deltaTime * smoothSpeed);
                
                if (_currentVelocity.sqrMagnitude > 0.1f) {
                    Quaternion tilt = Quaternion.LookRotation(Vector3.forward + _currentVelocity * 0.05f, Vector3.up);
                    _heldObject.transform.rotation = Quaternion.Slerp(_heldObject.transform.rotation, tilt, Time.deltaTime * 10f);
                }
            }
        }

        private void Release()
        {
            if (_heldObject == null) return;
            float speed = _currentVelocity.magnitude;
            var villager = _heldObject.GetComponent<VillagerController>();

            Debug.Log($"[GodHand] RELEASE! Speed: {speed:F2} | Threshold: {flingThresholdForced}");

            if (speed >= flingThresholdForced)
            {
                Debug.Log($"[GodHand] SUCCESS: Fling triggered!");
                if (villager != null) {
                    villager.SetSocialState(VillagerController.VillagerState.DivineProjectile);
                    if (speed > ragdollThreshold) {
                        villager.EnableRagdoll(true);
                    }
                }
                if (_heldRb != null) {
                    _heldRb.isKinematic = false; 
                    _heldRb.useGravity = true;
                    
                    Vector3 launchVelocity = _currentVelocity * flingForceMultiplier;
                    launchVelocity += Vector3.up * (speed * upwardForceBoost);
                    launchVelocity = Vector3.ClampMagnitude(launchVelocity, maxFlingForce);
                    
                    _heldRb.linearVelocity = launchVelocity; 
                    Debug.Log($"[GodHand] Applied launch velocity: {launchVelocity}");
                }
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("YEET!", _heldObject.transform.position + Vector3.up * 2f, Color.magenta);
            }
            else
            {
                Debug.Log($"[GodHand] FAIL: Speed {speed:F2} too low. Gentle release.");
                if (villager != null)
                {
                    villager.SetSocialState(VillagerController.VillagerState.DivineProjectile);
                }

                if (_heldRb != null)
                {
                    _heldRb.isKinematic = false;
                    _heldRb.useGravity = true;
                    _heldRb.linearVelocity = _currentVelocity;
                    _heldRb.linearDamping = 2f; // Un po' più veloce di prima
                }
            }
            _heldObject = null; _heldAgent = null; _heldRb = null; _isDragging = false;
        }

        private bool GetInputDown(out Vector2 pos) { if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { pos = Mouse.current.position.ReadValue(); return true; } if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; } pos = Vector2.zero; return false; }
        private bool GetInputHeld(out Vector2 pos) { if (Mouse.current != null && Mouse.current.leftButton.isPressed) { pos = Mouse.current.position.ReadValue(); return true; } if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; } pos = Vector2.zero; return false; }
        private bool GetInputUp(out Vector2 pos) { if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) { pos = Mouse.current.position.ReadValue(); return true; } if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) { pos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; } pos = Vector2.zero; return false; }
        private Vector2 GetInputScreenPos() { if (Pointer.current != null) return Pointer.current.position.ReadValue(); if (Mouse.current != null) return Mouse.current.position.ReadValue(); if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue(); return Vector2.zero; }
    }
}
