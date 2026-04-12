using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// HUD sopra la testa del villager: stato corrente + barra energia.
    /// Si auto-costruisce a runtime, ruota sempre verso la camera (billboard).
    /// Canvas in world-space: sizeDelta in px, localScale 0.01 → px diventano world units.
    /// </summary>
    [RequireComponent(typeof(VillagerController))]
    public class VillagerEnergyBar : MonoBehaviour
    {
        [Header("Layout")]
        public float heightOffset = 2.6f;   // unita' sopra il pivot del villager

        private VillagerController _villager;
        private Camera             _cam;
        private Transform          _barRoot;
        private RectTransform      _fill;
        private Text               _stateLabel;

        private void Start()
        {
            _villager = GetComponent<VillagerController>();
            _cam      = Camera.main;
            BuildHUD();
        }

        private void LateUpdate()
        {
            if (_barRoot == null || _cam == null) return;

            // Billboard: ruota verso la camera
            _barRoot.rotation = Quaternion.LookRotation(
                _barRoot.position - _cam.transform.position
            );

            if (_villager == null) return;

            // Barra energia
            if (_fill != null)
            {
                float ratio = _villager.Energy / _villager.maxEnergy;
                _fill.anchorMax = new Vector2(ratio, 1f);
                var img = _fill.GetComponent<Image>();
                if (img != null)
                    img.color = Color.Lerp(Color.red, Color.green, ratio);
            }

            // Label: [Personality] State | Loyalty: XX | Energy: YY
            if (_stateLabel != null)
                _stateLabel.text = $"{_villager.GetStateLabel()} | Energy: {Mathf.RoundToInt(_villager.Energy)}";
        }

        private void BuildHUD()
        {
            // Root billboard (posizionata sopra la testa)
            var rootGO = new GameObject("VillagerHUDRoot");
            rootGO.transform.SetParent(transform, false);
            rootGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            _barRoot = rootGO.transform;

            // Canvas world-space — sizeDelta in "px", scale 0.01 → 1px = 0.01 world unit
            // Pannello finale: 220px × 50px (piu' largo per info extra)
            var canvasGO = new GameObject("HUDCanvas");
            canvasGO.transform.SetParent(rootGO.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.worldCamera = _cam;
            var canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta    = new Vector2(240f, 50f);
            canvasRT.localPosition = Vector3.zero;
            canvasRT.localScale   = Vector3.one * 0.01f;

            // Sfondo pannello
            CreateImage(canvasGO.transform, "BG",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.7f));

            // Label stato (superiore)
            var labelGO = new GameObject("StateLabel");
            labelGO.transform.SetParent(canvasGO.transform, false);
            labelGO.AddComponent<CanvasRenderer>();
            _stateLabel = labelGO.AddComponent<Text>();
            _stateLabel.font      = GetFont();
            _stateLabel.fontSize  = 14;
            _stateLabel.color     = Color.white;
            _stateLabel.alignment = TextAnchor.MiddleCenter;
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin  = new Vector2(0f, 0.35f);
            labelRT.anchorMax  = new Vector2(1f, 1f);
            labelRT.offsetMin  = new Vector2(4f, 0f);
            labelRT.offsetMax  = new Vector2(-4f, 0f);

            // Sfondo barra (inferiore)
            var barBgGO = CreateImage(canvasGO.transform, "BarBG",
                new Vector2(0f, 0f), new Vector2(1f, 0.3f),
                new Vector2(6f, 4f), new Vector2(-6f, -4f),
                new Color(0.15f, 0.15f, 0.15f, 1f));

            // Fill barra
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(barBgGO.transform, false);
            fillGO.AddComponent<CanvasRenderer>();
            fillGO.AddComponent<Image>().color = Color.green;
            _fill = fillGO.GetComponent<RectTransform>();
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = Vector2.one;
            _fill.offsetMin = new Vector2(1f, 1f);
            _fill.offsetMax = new Vector2(-1f, -1f);
        }

        private static GameObject CreateImage(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static Font GetFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
