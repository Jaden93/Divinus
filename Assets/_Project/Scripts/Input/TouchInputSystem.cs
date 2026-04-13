using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DivinePrototype
{
    /// <summary>
    /// Rileva tap/click su terreno, NPC o risorse tramite raycast.
    /// Funziona con mouse (editor) e touch (Android).
    /// </summary>
    public class TouchInputSystem : MonoBehaviour
    {
        [Header("Raycast")]
        public LayerMask terrainLayer = ~0;
        public LayerMask villagerLayer = ~0;
        public float maxRayDistance = 200f;

        [Header("Feedback visivo")]
        public GameObject tapFeedbackPrefab;
        public float feedbackDuration = 0.5f;

        [Header("Events")]
        public UnityEvent<Vector3> onTerrainTapped = new UnityEvent<Vector3>();
        public UnityEvent<VillagerController> onVillagerTapped = new UnityEvent<VillagerController>();
        public UnityEvent<DamageableObject> onObjectTapped = new UnityEvent<DamageableObject>();
        public UnityEvent<ResourceNode> onResourceTapped = new UnityEvent<ResourceNode>();

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (GetTapThisFrame(out Vector2 screenPos))
                ProcessTap(screenPos);
        }

        private bool GetTapThisFrame(out Vector2 screenPos)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }
            screenPos = Vector2.zero;
            return false;
        }

        private void ProcessTap(Vector2 screenPos)
        {
            if (_camera == null) return;

            Ray ray = _camera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                Debug.Log($"[TouchInput] Hit: {hit.collider.name} su layer {hit.collider.gameObject.layer}");

                // 1. PRIORITA': Villager
                var villager = hit.collider.GetComponentInParent<VillagerController>();
                if (villager != null)
                {
                    SpawnFeedback(hit.point);
                    onVillagerTapped.Invoke(villager);
                    return;
                }

                // 2. PRIORITA': Risorse (Alberi/Rocce)
                var node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    Debug.Log($"[TouchInput] Nodo Risorsa rilevato: {node.name}");
                    SpawnFeedback(hit.point);
                    onResourceTapped.Invoke(node);
                    return;
                }

                // 3. PRIORITA': Oggetti danneggiabili (Edifici)
                var dmg = hit.collider.GetComponentInParent<DamageableObject>();
                if (dmg != null)
                {
                    SpawnFeedback(hit.point);
                    onObjectTapped.Invoke(dmg);
                    return;
                }
            }

            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                SpawnFeedback(worldPos);
                onTerrainTapped.Invoke(worldPos);
            }
        }

        private void SpawnFeedback(Vector3 worldPos)
        {
            if (tapFeedbackPrefab == null) return;
            var fx = Instantiate(tapFeedbackPrefab, worldPos + Vector3.up * 0.05f, Quaternion.identity);
            Destroy(fx, feedbackDuration);
        }
    }
}
