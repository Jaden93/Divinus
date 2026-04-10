using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Icona ascia.
    /// Modalità menu (nuova): StartAssignmentMode() → tap su villager = dona ascia,
    ///                                                tap su terreno = lascia ascia a terra.
    /// Modalità drag (mantenuta): drag → drop su villager o terreno.
    /// </summary>
    public class AxeActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Riferimenti")]
        public DivineActionSystem divineAction;
        public GameStateSystem    gameState;
        public Camera             mainCamera;
        public Canvas             rootCanvas;

        [Header("Ascia 3D drag")]
        public GameObject axePrefab3D;
        public float      dragHeightY = 1.5f;

        [Header("Drop detection")]
        public float dropRadius = 2.5f;

        [Header("Ascia a terra")]
        public GameObject axePickupPrefab;

        // Interni
        private GameObject         _axeInstance3D;
        private GameObject         _ghostUI;
        private bool               _dragging;
        private bool               _assignmentActive;
        private CircularMenuUI     _parentCircMenu;
        private VillagerController _hoveredVillager;
        private Color              _originalVillagerColor;
        private static readonly Color _hoverColor   = new Color(0.3f, 1f, 0.3f);
        private static readonly Plane _groundPlane  = new Plane(Vector3.up, Vector3.zero);

        private Image       _iconImage;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            if (gameState    == null) gameState    = FindObjectOfType<GameStateSystem>();
            if (mainCamera   == null) mainCamera   = Camera.main;
            if (rootCanvas   == null) rootCanvas   = GetComponentInParent<Canvas>();

            _iconImage   = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            SetDisabled(false);
        }

        // ── Assignment mode (da menu circolare) ─────────────────────────

        /// <summary>
        /// Attiva modalità assegnazione: il prossimo tap assegna l'ascia.
        /// </summary>
        public void StartAssignmentMode()
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _assignmentActive = true;
            Debug.Log("[AxeActionUI] Modalità assegnazione attiva. Tocca un villager o il terreno.");
        }

        private void Update()
        {
            if (!_assignmentActive) return;

            Vector2 screenPos = GetInputScreenPos();

            if (WasTappedOnWorld(screenPos))
            {
                _assignmentActive = false;

                VillagerController target = FindVillagerAtScreen(screenPos);
                if (target != null)
                {
                    if (divineAction != null) divineAction.GiveAxe(target);
                    target.HasPersonalAxe = true;
                    SetDisabled(true);
                    Debug.Log("[AxeActionUI] Ascia assegnata a " + target.name);
                }
                else
                {
                    SpawnAxePickup(screenPos);
                    SetDisabled(true);
                }
            }
        }

        // ── Drag classico (mantenuto) ────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _dragging = true;

            // Notifica il menu circolare: nasconde overlay + altri item senza interrompere il drag
            _parentCircMenu = GetComponentInParent<CircularMenuUI>();
            _parentCircMenu?.OnItemBeginDrag(GetComponent<RectTransform>());

            _ghostUI = new GameObject("AxeDragGhost");
            _ghostUI.transform.SetParent(rootCanvas.transform, false);
            var ghostImg = _ghostUI.AddComponent<Image>();
            ghostImg.sprite        = _iconImage != null ? _iconImage.sprite : null;
            ghostImg.color         = new Color(1f, 0.85f, 0.2f, 0.75f);
            ghostImg.raycastTarget = false;
            _ghostUI.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 70f);
            UpdateGhostUIPosition(eventData.position);

            if (axePrefab3D != null)
            {
                _axeInstance3D = Instantiate(axePrefab3D);
                foreach (var col in _axeInstance3D.GetComponentsInChildren<Collider>())
                    Destroy(col);
            }
            else
            {
                _axeInstance3D = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _axeInstance3D.transform.localScale = Vector3.one * 0.35f;
                _axeInstance3D.GetComponent<Renderer>().material.color = new Color(0.85f, 0.55f, 0.1f);
                Destroy(_axeInstance3D.GetComponent<Collider>());
            }
            Update3DAxePosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            UpdateGhostUIPosition(eventData.position);
            if (_axeInstance3D != null)
            {
                Update3DAxePosition(eventData.position);
                _axeInstance3D.transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
            }

            VillagerController found = FindVillagerAtScreen(eventData.position);
            if (found != _hoveredVillager)
            {
                ClearHover();
                if (found != null) ApplyHover(found);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            if (_ghostUI       != null) { Destroy(_ghostUI);       _ghostUI       = null; }
            if (_axeInstance3D != null) { Destroy(_axeInstance3D); _axeInstance3D = null; }
            ClearHover();

            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            if (divineAction == null || (gameState != null && gameState.HasAxe)) return;

            VillagerController target = FindVillagerAtScreen(eventData.position);
            if (target != null)
            {
                divineAction.GiveAxe(target);
                target.HasPersonalAxe = true;
                SetDisabled(true);
            }
            else
            {
                SpawnAxePickup(eventData.position);
                SetDisabled(true);
            }

            // Chiudi il menu circolare ora che il drag è terminato
            _parentCircMenu?.OnItemEndDrag();
            _parentCircMenu = null;
        }

        // ── Stato icona ──────────────────────────────────────────────────

        private void SetDisabled(bool disabled)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = !disabled;
            _canvasGroup.alpha        = disabled ? 0.35f : 1f;
        }

        public void OnWorkFinished() { SetDisabled(false); }

        // ── Hover ────────────────────────────────────────────────────────

        private void ApplyHover(VillagerController villager)
        {
            _hoveredVillager = villager;
            var smr = villager.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) { _originalVillagerColor = smr.material.color; smr.material.color = _hoverColor; }
        }

        private void ClearHover()
        {
            if (_hoveredVillager == null) return;
            var smr = _hoveredVillager.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null) smr.material.color = _originalVillagerColor;
            _hoveredVillager = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────

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

        private void Update3DAxePosition(Vector2 screenPos)
        {
            Vector3 world = ScreenToGround(screenPos);
            world.y = dragHeightY;
            _axeInstance3D.transform.position = world;
        }

        private VillagerController FindVillagerAtScreen(Vector2 screenPos)
        {
            if (mainCamera == null) return null;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.SphereCast(ray, dropRadius, out RaycastHit hit, 300f))
            {
                var v = hit.collider.GetComponentInParent<VillagerController>();
                if (v != null) return v;
            }

            Vector3 worldPos = ScreenToGround(screenPos);
            VillagerController best = null;
            float minDist = dropRadius * 2f;
            foreach (var v in FindObjectsOfType<VillagerController>())
            {
                float d = Vector3.Distance(v.transform.position, worldPos);
                if (d < minDist) { minDist = d; best = v; }
            }
            return best;
        }

        private void SpawnAxePickup(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;
            worldPos.y = 0.2f;

            GameObject prefab = axePickupPrefab != null ? axePickupPrefab :
                                axePrefab3D     != null ? axePrefab3D     : null;
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, worldPos, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position   = worldPos;
                go.transform.localScale = Vector3.one * 0.3f;
                go.GetComponent<Renderer>().material.color = new Color(0.85f, 0.55f, 0.1f);
                Destroy(go.GetComponent<Collider>());
            }

            go.name = "AxePickup";
            foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
            go.AddComponent<AxePickup>();

            if (gameState != null) gameState.HasAxe = true;
            Debug.Log("[AxeActionUI] Ascia lasciata a terra su " + worldPos);
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
