using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Icona Piccone.
    /// Simile ad AxeActionUI, gestisce il drag&drop o il tap per assegnare un piccone
    /// a un villager o lasciarlo a terra come PickaxePickup.
    /// </summary>
    public class PickaxeActionUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Riferimenti")]
        public DivineActionSystem divineAction;
        public Camera             mainCamera;
        public Canvas             rootCanvas;

        [Header("Piccone 3D drag")]
        public GameObject pickaxePrefab3D;
        public float      dragHeightY = 1.5f;

        [Header("Drop detection")]
        public float dropRadius = 2.5f;

        [Header("Piccone a terra")]
        public GameObject pickaxePickupPrefab;

        // Interni
        private GameObject         _pickaxeInstance3D;
        private GameObject         _ghostUI;
        private bool               _dragging;
        private bool               _assignmentActive;
        private CircularMenuUI     _parentCircMenu;
        private VillagerController _hoveredVillager;
        private Color              _originalVillagerColor;
        private static readonly Color _hoverColor   = new Color(0.3f, 0.8f, 1f);
        private static readonly Plane _groundPlane  = new Plane(Vector3.up, Vector3.zero);

        private Image       _iconImage;
        private CanvasGroup _canvasGroup;

        private void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            if (mainCamera   == null) mainCamera   = Camera.main;
            if (rootCanvas   == null) rootCanvas   = GetComponentInParent<Canvas>();

            _iconImage   = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Per ora lo lasciamo sempre attivo, o lo sblocchiamo con StoneDepot
        }

        public void StartAssignmentMode()
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _assignmentActive = true;
            Debug.Log("[PickaxeActionUI] Modalità assegnazione attiva. Tocca un villager o il terreno.");
        }

        private void Update()
        {
            if (!_assignmentActive) return;

            if (GetTapThisFrame(out Vector2 screenPos))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                _assignmentActive = false;

                VillagerController target = FindVillagerAtScreen(screenPos);
                if (target != null)
                {
                    if (divineAction != null) divineAction.GivePickaxe(target);
                    target.HasPersonalPickaxe = true;
                    Debug.Log("[PickaxeActionUI] Piccone assegnato a " + target.name);
                }
                else
                {
                    SpawnPickaxePickup(screenPos);
                }
            }
        }

        // ── Drag & Drop ──────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null && !_canvasGroup.interactable) return;
            _dragging = true;

            _parentCircMenu = GetComponentInParent<CircularMenuUI>();
            _parentCircMenu?.OnItemBeginDrag(GetComponent<RectTransform>());

            _ghostUI = new GameObject("PickaxeDragGhost");
            _ghostUI.transform.SetParent(rootCanvas.transform, false);
            var ghostImg = _ghostUI.AddComponent<Image>();
            ghostImg.sprite        = _iconImage != null ? _iconImage.sprite : null;
            ghostImg.color         = new Color(0.2f, 0.8f, 1f, 0.75f);
            ghostImg.raycastTarget = false;
            _ghostUI.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 70f);
            UpdateGhostUIPosition(eventData.position);

            if (pickaxePrefab3D != null)
            {
                _pickaxeInstance3D = Instantiate(pickaxePrefab3D);
                foreach (var col in _pickaxeInstance3D.GetComponentsInChildren<Collider>())
                    Destroy(col);
            }
            else
            {
                _pickaxeInstance3D = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _pickaxeInstance3D.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
                _pickaxeInstance3D.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 0.9f);
                Destroy(_pickaxeInstance3D.GetComponent<Collider>());
            }
            Update3DPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            UpdateGhostUIPosition(eventData.position);
            if (_pickaxeInstance3D != null)
            {
                Update3DPosition(eventData.position);
                _pickaxeInstance3D.transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
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

            if (_ghostUI           != null) { Destroy(_ghostUI);           _ghostUI = null; }
            if (_pickaxeInstance3D != null) { Destroy(_pickaxeInstance3D); _pickaxeInstance3D = null; }
            ClearHover();

            if (divineAction == null) return;

            VillagerController target = FindVillagerAtScreen(eventData.position);
            if (target != null)
            {
                divineAction.GivePickaxe(target);
                target.HasPersonalPickaxe = true;
            }
            else
            {
                SpawnPickaxePickup(eventData.position);
            }

            _parentCircMenu?.OnItemEndDrag();
            _parentCircMenu = null;
        }

        // ── Hover & Visuals ──────────────────────────────────────────────

        private void ApplyHover(VillagerController villager)
        {
            _hoveredVillager = villager;
            var smr = villager.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) smr = villager.GetComponentInChildren<Renderer>() as SkinnedMeshRenderer;
            if (smr != null) { _originalVillagerColor = smr.material.color; smr.material.color = _hoverColor; }
        }

        private void ClearHover()
        {
            if (_hoveredVillager == null) return;
            var smr = _hoveredVillager.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) smr = _hoveredVillager.GetComponentInChildren<Renderer>() as SkinnedMeshRenderer;
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

        private void UpdateGhostUIPosition(Vector2 screenPos)
        {
            if (_ghostUI == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform, screenPos,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out Vector2 localPos);
            _ghostUI.GetComponent<RectTransform>().localPosition = localPos;
        }

        private void Update3DPosition(Vector2 screenPos)
        {
            Vector3 world = ScreenToGround(screenPos);
            world.y = dragHeightY;
            _pickaxeInstance3D.transform.position = world;
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
            return null;
        }

        private void SpawnPickaxePickup(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToGround(screenPos);
            if (worldPos == Vector3.zero) return;
            worldPos.y = 0.2f;

            GameObject go;
            if (pickaxePickupPrefab != null)
            {
                go = Instantiate(pickaxePickupPrefab, worldPos, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.position   = worldPos;
                go.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                go.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 0.9f);
                foreach(var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            }

            go.name = "PickaxePickup";
            go.AddComponent<PickaxePickup>();
            Debug.Log("[PickaxeActionUI] Piccone lasciato a terra su " + worldPos);
        }

        private Vector3 ScreenToGround(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector3.zero;
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            return _groundPlane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}
