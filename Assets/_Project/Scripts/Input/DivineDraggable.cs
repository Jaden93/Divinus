using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace DivinePrototype
{
    /// <summary>
    /// Gestisce il Drag & Drop e il Lancio direttamente sul Villager.
    /// Usa i metodi nativi Unity per la massima precisione.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DivineDraggable : MonoBehaviour
    {
        [Header("Configurazione")]
        public float pickupHeight = 2.5f;
        public float longPressThreshold = 0.25f;
        public float flingForceMultiplier = 1.2f;
        public float throwVelocityThreshold = 5f;

        private Camera _mainCamera;
        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private VillagerController _villager;

        private bool _isDragging = false;
        private float _mouseDownTime;
        private Vector3 _lastWorldPos;
        private Vector3 _currentVelocity;
        private Plane _dragPlane;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
            _villager = GetComponent<VillagerController>();
        }

        private void OnMouseDown()
        {
            // Blocca se stiamo cliccando su UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            _mouseDownTime = Time.time;
            _isDragging = false;
            
            // Inizializza il piano di trascinamento all'altezza del pickup
            _dragPlane = new Plane(Vector3.up, new Vector3(0, pickupHeight, 0));
            _lastWorldPos = transform.position;
        }

        private void OnMouseDrag()
        {
            float pressDuration = Time.time - _mouseDownTime;

            if (!_isDragging && pressDuration > longPressThreshold)
            {
                StartGrab();
            }

            if (_isDragging)
            {
                UpdateDrag();
            }
        }

        private void OnMouseUp()
        {
            if (_isDragging)
            {
                Release();
            }
            else
            {
                // Se il tocco è stato breve, è un Tap (Selezione)
                if (Time.time - _mouseDownTime < longPressThreshold)
                {
                    if (DivineSelectionSystem.Instance != null && _villager != null)
                    {
                        DivineSelectionSystem.Instance.SelectVillager(_villager);
                    }
                }
            }
            
            _isDragging = false;
        }

        private void StartGrab()
        {
            _isDragging = true;
            
            if (_agent != null) _agent.enabled = false;
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("GRAB!", transform.position + Vector3.up * 2f, Color.cyan);
        }

        private void UpdateDrag()
        {
            // Usa Input.mousePosition assoluto come richiesto
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPoint = ray.GetPoint(enter);
                
                // Calcola velocità per il lancio
                if (Time.deltaTime > 0)
                {
                    _currentVelocity = (worldPoint - _lastWorldPos) / Time.deltaTime;
                    _lastWorldPos = worldPoint;
                }

                // Spostamento diretto (senza lerp per massima precisione di puntamento)
                transform.position = worldPoint;
                
                // Rotazione verso la direzione di movimento
                if (_currentVelocity.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(_currentVelocity.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                }
            }
        }

        private void Release()
        {
            bool isThrow = _currentVelocity.magnitude > throwVelocityThreshold;

            if (isThrow && _villager != null)
            {
                if (_rb != null)
                {
                    _rb.isKinematic = false;
                    _rb.useGravity = true;
                    _rb.AddForce(_currentVelocity * flingForceMultiplier, ForceMode.Impulse);
                }
                _villager.SetSocialState(VillagerController.VillagerState.DivineProjectile);
                
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("YEET!", transform.position + Vector3.up * 1f, Color.magenta);
            }
            else
            {
                // Drop normale
                if (_agent != null) _agent.enabled = true;
                if (_rb != null) _rb.isKinematic = true;
                if (_villager != null) _villager.GoIdleDirect();
            }
        }
    }
}
