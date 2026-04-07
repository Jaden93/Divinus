using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Pulsante HOME.
    /// Modalità classica: drag → drop.
    /// Modalità menu (nuova): StartPlacementMode() → preview segue dito → tap per piazzare.
    /// </summary>
    public class HouseActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Riferimenti")]
        public GameStateSystem  gameState;
        public ConstructionSite constructionSite;
        public Camera           mainCamera;
        public Canvas           rootCanvas;

        [Header("Preview casa")]
        public GameObject housePrefab;
        public float      previewHeightY = 0f;

        // Interni
        private GameObject  _previewInstance;
        private GameObject  _ghostUI;
        private bool        _dragging;
        private bool        _placementActive;
        private bool        _placementJustActivated;   // skip del frame in cui è stata attivata la modalità
        private CircularMenuUI _parentCircMenu;
        private static readonly Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);

        private Image       _iconImage;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (gameState        == null) gameState        = FindObjectOfType<GameStateSystem>();
            if (constructionSite == null) constructionSite = FindObjectOfType<ConstructionSite>();
            if (mainCamera       == null) mainCamera       = Camera.main;
            if (rootCanvas       == null) rootCanvas       = GetComponentInParent<Canvas>();

            _iconImage   = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetEnabled(false);

            var depot = FindObjectOfType<WoodDepot>();
            if (depot != null)
            {
                depot.onConstructionReady.AddListener(OnConstructionReady);
                depot.onWoodDeposited.AddListener(_ => RefreshEnabled());
                if (depot.IsConstructionReady) OnConstructionReady();
            }

            var gs = FindObjectOfType<GameStateSystem>();
            if (gs != null)
                gs.onWoodChanged.AddListener(_ => RefreshEnabled());
        }

        private void OnConstructionReady() { SetEnabled(true); }

        private void RefreshEnabled()
        {
            var depot = WoodDepot.Instance;
            if (depot == null) return;
            SetEnabled(depot.IsConstructionReady);
        }

        // ── Placement mode (da menu circolare) ──────────────────────────

        /// <summary>
        /// Attiva la modalità piazzamento: una preview 3D segue il tocco.
        /// Il tap sul mondo conferma il posizionamento.
        /// </summary>
        public void StartPlacementMode()
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            if (_placementActive) return;

            _placementActive = true;
            _placementJustActivated = true;
            GridManager.Instance?.ShowGrid();

            if (housePrefab != null)
            {
                _previewInstance = Instantiate(housePrefab);
                DisableGameplayComponents(_previewInstance);
                _previewInstance.SetActive(true);
                SetPreviewTransparent(_previewInstance, 0.4f);
            }
        }

        private void Update()
        {
            if (!_placementActive) return;

            // Salta il frame in cui è stata attivata la placement mode per non
            // consumare subito il tap del menu come tap di conferma
            if (_placementJustActivated) { _placementJustActivated = false; return; }

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

            if (constructionSite != null && constructionSite.IsBuilding)
            {
                Debug.Log("[HouseActionUI] Costruzione già in corso.");
                return;
            }

            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[HouseActionUI] Posizione bloccata, piazzamento annullato.");
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            if (IsVillagerNearby(worldPos, 1.5f))
            {
                Debug.Log("[HouseActionUI] Villager vicino, posizione non valida.");
                GridManager.Instance?.FreeCell(worldPos);
                return;
            }

            if (constructionSite != null)
            {
                constructionSite.StartConstruction(worldPos);
                SetEnabled(false);
                Debug.Log("[HouseActionUI] Costruzione avviata su " + worldPos);
            }
        }

        // ── Drag classico (mantenuto per compatibilità) ──────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _dragging = true;

            _parentCircMenu = GetComponentInParent<CircularMenuUI>();
            _parentCircMenu?.OnItemBeginDrag(GetComponent<RectTransform>());

            GridManager.Instance?.ShowGrid();

            if (housePrefab != null)
            {
                _previewInstance = Instantiate(housePrefab);
                DisableGameplayComponents(_previewInstance);
                _previewInstance.SetActive(true);
                SetPreviewTransparent(_previewInstance, 0.4f);
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
            if (constructionSite != null && constructionSite.IsBuilding)
            {
                Debug.Log("[HouseActionUI] Costruzione già in corso.");
                _parentCircMenu?.OnItemEndDrag(); _parentCircMenu = null;
                return;
            }

            Vector3 worldPos = ScreenToGround(eventData.position);
            if (worldPos == Vector3.zero) return;

            GridManager.Instance?.HideGrid();

            if (GridManager.Instance != null)
            {
                worldPos = GridManager.Instance.SnapToGrid(worldPos);
                if (GridManager.Instance.IsPositionBlocked(worldPos))
                {
                    Debug.Log("[HouseActionUI] Posizione bloccata, drop annullato.");
                    return;
                }
                GridManager.Instance.OccupyCell(worldPos);
            }

            if (IsVillagerNearby(worldPos, 1.5f))
            {
                Debug.Log("[HouseActionUI] Villager nel punto di costruzione, drop annullato.");
                GridManager.Instance?.FreeCell(worldPos);
                return;
            }

            if (constructionSite != null)
            {
                constructionSite.StartConstruction(worldPos);
                SetEnabled(false);
                Debug.Log("[HouseActionUI] Costruzione avviata su " + worldPos);
            }

            _parentCircMenu?.OnItemEndDrag();
            _parentCircMenu = null;
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

        private void UpdateGhostUIPosition(Vector2 screenPos)
        {
            if (_ghostUI == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform, screenPos,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out Vector2 localPos);
            _ghostUI.GetComponent<RectTransform>().localPosition = localPos;
        }

        /// <summary>Disabilita i componenti di gameplay sull'istanza preview per evitare effetti collaterali.</summary>
        private void DisableGameplayComponents(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<MonoBehaviour>())
            {
                if (c is HouseController || c is HouseTransparency)
                    c.enabled = false;
            }
            // Rimuovi collider per evitare interferenze con IsPositionBlocked
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;
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
            Vector3 world = ScreenToGround(screenPos);
            if (world == Vector3.zero) return;

            if (GridManager.Instance != null)
            {
                world = GridManager.Instance.SnapToGrid(world);
                bool blocked = GridManager.Instance.IsPositionBlocked(world);
                TintPreview(blocked ? new Color(1f, 0.2f, 0.2f, 0.5f) : new Color(0.4f, 1f, 0.4f, 0.4f));
            }
            world.y = previewHeightY;
            _previewInstance.transform.position = world;

            // Ruota il preview così la porta guarda verso la camera
            if (mainCamera != null)
            {
                Vector3 camFwd = mainCamera.transform.forward;
                camFwd.y = 0f;
                if (camFwd.sqrMagnitude > 0.001f)
                    _previewInstance.transform.rotation = Quaternion.LookRotation(camFwd.normalized);
            }
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }

        private static bool IsVillagerNearby(Vector3 pos, float radius)
        {
            foreach (var col in Physics.OverlapSphere(pos, radius))
                if (col.GetComponent<VillagerController>() != null) return true;
            return false;
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
    }
}
