using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DivinePrototype
{
    /// <summary>
    /// Quando Smite è attivo:
    ///  - 4 barre rosse pulsanti sui bordi schermo
    ///  - crosshair rosso al centro
    ///  - hover rosso sull'oggetto DamageableObject sotto il cursore
    /// </summary>
    public class SmiteIndicatorUI : MonoBehaviour
    {
        [Header("Riferimenti (auto-trovati se null)")]
        public DivineActionSystem divineAction;
        public Camera mainCamera;

        [Header("Pulse")]
        public float pulseSpeed = 2.5f;
        public float minAlpha   = 0.20f;
        public float maxAlpha   = 0.60f;

        // Edge bars + crosshair
        private Image[] _edges = new Image[4];  // top, bot, left, right
        private Image   _crossH, _crossV;
        private bool    _smiteActive;

        // Hover
        private DamageableObject _hoveredObj;
        private Material[][]     _origMats;
        private Material         _hoverMat;

        void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            if (mainCamera   == null) mainCamera   = Camera.main;

            _hoverMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _hoverMat.SetColor("_BaseColor", new Color(1f, 0.12f, 0.05f, 1f));
            _hoverMat.SetFloat("_Smoothness", 0.05f);

            BuildUI();
            ShowUI(false);
        }

        void Update()
        {
            bool isSmite = divineAction != null && divineAction.PendingPower == DivinePower.Smite;

            if (isSmite != _smiteActive)
            {
                _smiteActive = isSmite;
                ShowUI(isSmite);
                if (!isSmite) ClearHover();
            }

            if (!_smiteActive) return;

            // Pulse color
            float a = Mathf.Lerp(minAlpha, maxAlpha,
                (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
            Color c = new Color(0.92f, 0.05f, 0.05f, a);
            foreach (var e in _edges) if (e != null) e.color = c;

            // Hover detect
            UpdateHover();
        }

        void OnDestroy()
        {
            ClearHover();
            if (_hoverMat != null) Destroy(_hoverMat);
        }

        // ── UI ───────────────────────────────────────────────────────────

        private void ShowUI(bool show)
        {
            foreach (var e in _edges) if (e != null) e.gameObject.SetActive(show);
            if (_crossH != null) _crossH.gameObject.SetActive(show);
            if (_crossV != null) _crossV.gameObject.SetActive(show);
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform;

            // Edge bars
            _edges[0] = EdgeBar("SmiteTop",   root, new Vector2(0,1), new Vector2(1,1), new Vector2(0,24),  new Vector2(0,-12));
            _edges[1] = EdgeBar("SmiteBot",   root, new Vector2(0,0), new Vector2(1,0), new Vector2(0,24),  new Vector2(0, 12));
            _edges[2] = EdgeBar("SmiteLeft",  root, new Vector2(0,0), new Vector2(0,1), new Vector2(20,0),  new Vector2(10,  0));
            _edges[3] = EdgeBar("SmiteRight", root, new Vector2(1,0), new Vector2(1,1), new Vector2(20,0),  new Vector2(-10, 0));

            // Crosshair
            _crossH = Bar("CrossH", root, new Vector2(32, 3));
            _crossV = Bar("CrossV", root, new Vector2(3, 32));
        }

        private Image EdgeBar(string n, Transform p, Vector2 aMin, Vector2 aMax, Vector2 sd, Vector2 ap)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.sizeDelta = sd; rt.anchoredPosition = ap;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.92f, 0.05f, 0.05f, 0.35f);
            return img;
        }

        private Image Bar(string n, Transform p, Vector2 size)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.08f, 0.08f, 0.95f);
            return img;
        }

        // ── Hover ────────────────────────────────────────────────────────

        private void UpdateHover()
        {
            if (mainCamera == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            DamageableObject hit = null;
            if (Physics.Raycast(ray, out RaycastHit rh, 200f))
                hit = rh.collider.GetComponentInParent<DamageableObject>();

            if (hit == _hoveredObj) return;
            ClearHover();
            if (hit != null && hit.CurrentState != DamageableObject.DamageState.Destroyed)
                ApplyHover(hit);
        }

        private void ApplyHover(DamageableObject obj)
        {
            _hoveredObj = obj;
            var renderers = obj.GetComponentsInChildren<Renderer>();
            _origMats = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                _origMats[i] = renderers[i].sharedMaterials;
                var mats = new Material[renderers[i].sharedMaterials.Length];
                for (int j = 0; j < mats.Length; j++) mats[j] = _hoverMat;
                renderers[i].materials = mats;
            }
        }

        private void ClearHover()
        {
            if (_hoveredObj == null) return;
            var renderers = _hoveredObj.GetComponentsInChildren<Renderer>();
            if (_origMats != null)
                for (int i = 0; i < renderers.Length && i < _origMats.Length; i++)
                    if (renderers[i] != null)
                        renderers[i].sharedMaterials = _origMats[i];
            _hoveredObj = null;
            _origMats   = null;
        }
    }
}
