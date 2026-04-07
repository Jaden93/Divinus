using UnityEngine;
using UnityEngine.EventSystems;

namespace DivinePrototype
{
    /// <summary>
    /// Bottone DEPOT. Placement mode: tap per piazzare un depot legna.
    /// Costo: depotCost legna. Senza depot: cap 9. Con depot: cap 30.
    /// </summary>
    public class WoodDepotActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Costo")]
        public int depotCost = 5;

        [Header("Riferimenti")]
        public GameStateSystem gameState;
        public Camera          mainCamera;

        [Header("Prefab")]
        public GameObject depotPrefab;

        private GameObject  _previewInstance;
        private bool        _placementActive;
        private bool        _dragging;
        private CanvasGroup _canvasGroup;
        private static readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Start()
        {
            if (gameState  == null) gameState  = FindObjectOfType<GameStateSystem>();
            if (mainCamera == null) mainCamera = Camera.main;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetEnabled(false);

            if (gameState != null)
            {
                gameState.onWoodChanged.AddListener(OnWoodChanged);
                OnWoodChanged(gameState.WoodCount);
            }
        }

        private void OnDestroy()
        {
            if (gameState != null)
                gameState.onWoodChanged.RemoveListener(OnWoodChanged);
        }

        private void OnWoodChanged(int amount) => SetEnabled(amount >= depotCost);

        // ── Placement mode ───────────────────────────────────────────────

        public void StartPlacementMode()
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            if (_placementActive) return;
            _placementActive = true;
            GridManager.Instance?.ShowGrid();
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
            GridManager.Instance?.HideGrid();

            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[WoodDepotActionUI] Posizione bloccata.");
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            PlaceDepot(worldPos);
        }

        // ── Drag ────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _dragging = true;
            GridManager.Instance?.ShowGrid();
            SpawnPreview();
            UpdatePreviewPosition(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging) return;
            UpdatePreviewPosition(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging) return;
            _dragging = false;
            DestroyPreview();
            GridManager.Instance?.HideGrid();

            if (_canvasGroup != null && !_canvasGroup.interactable) return;

            Vector3 worldPos = ScreenToGround(e.position);
            if (worldPos == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[WoodDepotActionUI] Posizione bloccata.");
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            PlaceDepot(worldPos);
        }

        // ── Piazzamento ──────────────────────────────────────────────────

        private void PlaceDepot(Vector3 worldPos)
        {
            if (depotPrefab == null) { Debug.LogWarning("[WoodDepotActionUI] depotPrefab mancante."); return; }
            if (gameState == null || gameState.WoodCount < depotCost) { Debug.LogWarning("[WoodDepotActionUI] Legna insufficiente."); return; }

            worldPos.y = 0f;
            var depot = Object.Instantiate(depotPrefab, worldPos, Quaternion.identity);
            depot.SetActive(true);

            if (depot.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obs = depot.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.carving = true;
                obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
                obs.size    = new Vector3(2f, 2f, 2f);
                obs.center  = new Vector3(0f, 1f, 0f);
            }

            gameState.WoodCount -= depotCost;

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("Depot built!", worldPos, new Color(0.9f, 0.6f, 0.1f));

            Debug.Log("[WoodDepotActionUI] Depot piazzato a " + worldPos);
        }

        // ── Preview ──────────────────────────────────────────────────────

        private void SpawnPreview()
        {
            if (depotPrefab == null) return;
            _previewInstance = Instantiate(depotPrefab);
            foreach (var col in _previewInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;
            _previewInstance.SetActive(true);
            SetTransparent(_previewInstance, 0.4f);
        }

        private void DestroyPreview()
        {
            if (_previewInstance != null) { Destroy(_previewInstance); _previewInstance = null; }
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

        private void TintPreview(Color c)
        {
            if (_previewInstance == null) return;
            foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials) mat.color = c;
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
            { screenPos = touch.primaryTouch.position.ReadValue(); return true; }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            { screenPos = mouse.position.ReadValue(); return true; }
            screenPos = Vector2.zero; return false;
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

        private bool WasTappedOnWorld(Vector2 sp)
        {
            if (!GetTapThisFrame(out _)) return false;
            if (EventSystem.current != null) return !EventSystem.current.IsPointerOverGameObject();
            return true;
        }

        private Vector3 ScreenToGround(Vector2 sp)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(sp);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }

        private void SetTransparent(GameObject go, float alpha)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    Color c = mat.color; c.a = alpha; mat.color = c;
                }
        }
    }
}
