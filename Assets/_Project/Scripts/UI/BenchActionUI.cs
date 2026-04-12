using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Bottone BENCH.
    /// Modalità menu (nuova): StartPlacementMode() → preview segue dito → tap per piazzare.
    /// Costo: benchCost legna.
    /// </summary>
    public class BenchActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Costo")]
        public int benchCost = 3;

        [Header("Riferimenti")]
        public GameStateSystem gameState;
        public Camera          mainCamera;
        public Canvas          rootCanvas;

        [Header("Prefab")]
        public GameObject benchPrefab;

        private GameObject  _previewInstance;
        private GameObject  _ghostUI;
        private bool        _dragging;
        private bool        _placementActive;
        private CircularMenuUI _parentCircMenu;
        private static readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        private Image       _iconImage;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (gameState  == null) gameState  = FindObjectOfType<GameStateSystem>();
            if (mainCamera == null) mainCamera = Camera.main;
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();

            _iconImage   = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetEnabled(false);

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.wood.onChanged.AddListener(_ => OnResourcesChanged());
                ResourceManager.Instance.stone.onChanged.AddListener(_ => OnResourcesChanged());
                OnResourcesChanged();
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.wood.onChanged.RemoveListener(_ => OnResourcesChanged());
                ResourceManager.Instance.stone.onChanged.RemoveListener(_ => OnResourcesChanged());
            }
        }

        private void OnResourcesChanged()
        {
            if (ResourceManager.Instance == null) return;
            SetEnabled(ResourceManager.Instance.HasResources(benchCost, 0));
        }

        // ── Placement mode (da menu circolare) ──────────────────────────

        public void StartPlacementMode()
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            if (_placementActive) return;

            _placementActive = true;
            GridManager.Instance?.ShowGrid();

            if (benchPrefab != null)
            {
                _previewInstance = Instantiate(benchPrefab);
                DisablePreviewComponents(_previewInstance);
                _previewInstance.SetActive(true);
                SetTransparent(_previewInstance, 0.4f);
            }
        }

        private void Update()
        {
            if (!_placementActive) return;

            Vector2 screenPos = GetInputScreenPos();
            UpdatePreviewPosition(screenPos);

            if (WasTappedOnWorld(screenPos))
            {
                CommitPlacement(screenPos);
            }
        }

        private void CommitPlacement(Vector2 screenPos)
        {
            if (_previewInstance != null) { Destroy(_previewInstance); _previewInstance = null; }
            _placementActive = false;
            GridManager.Instance?.HideGrid();

            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[BenchActionUI] Posizione bloccata.");
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            PlaceBench(worldPos);
        }

        // ── Drag classico (mantenuto per compatibilità) ──────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _dragging = true;

            _parentCircMenu = GetComponentInParent<CircularMenuUI>();
            _parentCircMenu?.OnItemBeginDrag(GetComponent<RectTransform>());

            GridManager.Instance?.ShowGrid();

            if (benchPrefab != null)
            {
                _previewInstance = Instantiate(benchPrefab);
                DisablePreviewComponents(_previewInstance);
                _previewInstance.SetActive(true);
                SetTransparent(_previewInstance, 0.4f);
            }
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

            if (_previewInstance != null) { Destroy(_previewInstance); _previewInstance = null; }

            if (_canvasGroup != null && !_canvasGroup.interactable) return;

            Vector3 worldPos = ScreenToGround(eventData.position);
            if (worldPos == Vector3.zero) return;

            GridManager.Instance?.HideGrid();

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[BenchActionUI] Posizione bloccata, drop annullato.");
                    _parentCircMenu?.OnItemEndDrag();
                    _parentCircMenu = null;
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            PlaceBench(worldPos);

            _parentCircMenu?.OnItemEndDrag();
            _parentCircMenu = null;
        }

        // ── Piazzamento ──────────────────────────────────────────────────

        private void PlaceBench(Vector3 worldPos)
        {
            if (benchPrefab == null) { Debug.LogWarning("[BenchActionUI] benchPrefab non assegnato."); return; }

            if (ResourceManager.Instance != null)
            {
                if (!ResourceManager.Instance.HasResources(benchCost, 0))
                {
                    Debug.LogWarning("[BenchActionUI] Risorse insufficienti.");
                    return;
                }
                ResourceManager.Instance.SpendResource("Wood", benchCost);
            }
            else if (gameState != null)
            {
                if (gameState.WoodCount < benchCost) return;
                gameState.WoodCount -= benchCost;
            }

            worldPos.y = 0f;

            foreach (var col in Physics.OverlapSphere(worldPos, 1f))
                if (col.GetComponent<VillagerController>() != null) { Debug.LogWarning("[BenchActionUI] Villager vicino."); return; }

            var bench = Instantiate(benchPrefab, worldPos, Quaternion.identity);

            if (bench.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obs = bench.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.carving = true;
                obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
                obs.size    = new Vector3(1.5f, 1f, 0.8f);
                obs.center  = new Vector3(0f, 0.5f, 0f);
            }

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("Bench placed!", worldPos, new Color(0.8f, 0.6f, 0.2f));

            Debug.Log("[BenchActionUI] Panca piazzata a " + worldPos);
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void SetEnabled(bool enabled)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = enabled;
            _canvasGroup.alpha        = enabled ? 1f : 0.35f;
        }

        private bool GetTapThisFrame(out Vector2 screenPos)
        {
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                return true;
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }
            screenPos = Vector2.zero;
            return false;
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
            if (!GetTapThisFrame(out _)) return false;
            if (EventSystem.current != null)
                return !EventSystem.current.IsPointerOverGameObject();
            return true;
        }

        private void UpdateGhostPosition(Vector2 screenPos)
        {
            if (_ghostUI == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform, screenPos,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out Vector2 local);
            _ghostUI.GetComponent<RectTransform>().localPosition = local;
        }

        private void TintPreview(Color tint)
        {
            if (_previewInstance == null) return;
            foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                    mat.color = tint;
        }

        private void UpdatePreviewPosition(Vector2 screenPos)
        {
            if (_previewInstance == null) return;
            Vector3 w = ScreenToGround(screenPos);
            if (w == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                w = GridManager.Instance.SnapToGrid(w);
                bool blocked = GridManager.Instance.IsPositionBlocked(w);
                TintPreview(blocked ? new Color(1f, 0.2f, 0.2f, 0.5f) : new Color(0.4f, 1f, 0.4f, 0.4f));
            }

            w.y = 0f;
            _previewInstance.transform.position = w;
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }

        private void DisablePreviewComponents(GameObject go)
        {
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (Transform t in go.GetComponentsInChildren<Transform>())
                t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;

            foreach (var nav in go.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>())
                nav.enabled = false;

            foreach (var logic in go.GetComponentsInChildren<MonoBehaviour>())
            {
                if (logic != this) logic.enabled = false;
            }
        }

        private void SetTransparent(GameObject go, float alpha)
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
    }
}
