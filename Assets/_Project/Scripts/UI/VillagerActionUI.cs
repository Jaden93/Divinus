using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Icona nel menu divino. Drag → drop per spawnare un villager.
    /// Modalità placement (StartPlacementMode): preview 3D segue il dito → tap per piazzare.
    /// La preview appare trasparente con tint verde/rosso in base alla validità.
    /// </summary>
    public class VillagerActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Riferimenti")]
        public DivineActionSystem divineAction;
        public Camera             mainCamera;
        public Canvas             rootCanvas;

        [Header("Villager preview")]
        public GameObject villagerPrefab;
        public bool       isFemale = false;
        public float      placementRadius = 1.5f;   // raggio collision check

        // Runtime
        private GameObject     _previewInstance;
        private bool           _dragging;
        private bool           _placementActive;
        private CircularMenuUI _parentCircMenu;

        private static readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            if (mainCamera   == null) mainCamera   = Camera.main;
            if (rootCanvas   == null) rootCanvas   = GetComponentInParent<Canvas>();

            // Usa il prefab di DivineActionSystem se non assegnato manualmente
            if (villagerPrefab == null && divineAction != null)
                villagerPrefab = isFemale ? divineAction.villagerFemalePrefab : divineAction.villagerMalePrefab;
        }

        // ── Placement mode (da tap su item menu) ─────────────────────────

        public void StartPlacementMode()
        {
            if (_placementActive) return;
            _placementActive = true;
            SpawnPreview();
        }

        private void Update()
        {
            if (!_placementActive) return;

            Vector2 screenPos = GetInputScreenPos();
            UpdatePreviewPosition(screenPos);

            if (WasTappedOnWorld(screenPos))
                CommitPlacement(screenPos);
        }

        private void CommitPlacement(Vector2 screenPos)
        {
            DestroyPreview();
            _placementActive = false;

            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;
            if (IsPositionBlocked(worldPos))
            {
                Debug.Log("[VillagerActionUI] Posizione bloccata, piazzamento annullato.");
                return;
            }
            DoSpawn(worldPos);
        }

        // ── Drag classico ─────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _parentCircMenu = GetComponentInParent<CircularMenuUI>();
            _parentCircMenu?.OnItemBeginDrag(GetComponent<RectTransform>());
            SpawnPreview();
            UpdatePreviewPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            UpdatePreviewPosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            DestroyPreview();

            Vector3 worldPos = ScreenToGround(eventData.position);
            if (worldPos != Vector3.zero && !IsPositionBlocked(worldPos))
                DoSpawn(worldPos);
            else
                Debug.Log("[VillagerActionUI] Drop su posizione bloccata o non valida.");

            _parentCircMenu?.OnItemEndDrag();
            _parentCircMenu = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void SpawnPreview()
        {
            if (villagerPrefab == null) return;
            _previewInstance = Instantiate(villagerPrefab);
            DisableGameplayComponents(_previewInstance);
            SetPreviewTransparent(_previewInstance, 0.45f);
        }

        private void DestroyPreview()
        {
            if (_previewInstance != null) { Destroy(_previewInstance); _previewInstance = null; }
        }

        private void DoSpawn(Vector3 worldPos)
        {
            if (divineAction != null)
                divineAction.SpawnVillagerAt(isFemale, worldPos);
        }

        private void UpdatePreviewPosition(Vector2 screenPos)
        {
            if (_previewInstance == null) return;
            Vector3 world = ScreenToGround(screenPos);
            if (world == Vector3.zero) return;

            bool blocked = IsPositionBlocked(world);
            TintPreview(blocked
                ? new Color(1f, 0.2f, 0.2f, 0.5f)
                : new Color(0.5f, 1f, 0.5f, 0.45f));

            _previewInstance.transform.position = world;
        }

        private bool IsPositionBlocked(Vector3 pos)
        {
            if (GridManager.Instance != null && GridManager.Instance.IsPositionBlocked(pos))
                return true;

            foreach (var col in Physics.OverlapSphere(pos, placementRadius))
            {
                if (col.GetComponent<HouseController>()    != null) return true;
                if (col.GetComponent<ConstructionSite>()   != null) return true;
                if (col.GetComponent<BenchController>()    != null) return true;
            }
            return false;
        }

        private void DisableGameplayComponents(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
                t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            foreach (var c in go.GetComponentsInChildren<MonoBehaviour>())
                if (c != this) c.enabled = false;

            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var nav in go.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>())
                nav.enabled = false;
        }

        private void TintPreview(Color tint)
        {
            if (_previewInstance == null) return;
            foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                    mat.color = tint;
        }

        private void SetPreviewTransparent(GameObject go, float alpha)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    Color c = mat.color; c.a = alpha; mat.color = c;
                }
        }

        private Vector2 GetInputScreenPos()
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return touch.primaryTouch.position.ReadValue();
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null) return mouse.position.ReadValue();
            return Vector2.zero;
        }

        private bool WasTappedOnWorld(Vector2 screenPos)
        {
            bool pressed = false;
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) pressed = true;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) pressed = true;
            if (!pressed) return false;
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
