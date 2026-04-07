using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Pannello laterale SINISTRO con i poteri d'intervento divino:
    /// Smite, Repair, Revive. Separato dagli spawn per chiarezza di UX.
    /// </summary>
    public class PowersMenuUI : MonoBehaviour
    {
        [Header("Riferimenti (auto-trovati se null)")]
        public DivineActionSystem divineAction;

        [Header("Layout")]
        public float panelWidth  = 140f;
        public float toggleWidth = 28f;
        public float slideSpeed  = 10f;

        private class PowerEntry
        {
            public string       label;
            public string       description;
            public DivinePower  power;
            public Color        normalColor;
            public Color        selectedColor;
        }

        private readonly List<PowerEntry> _entries = new List<PowerEntry>
        {
            new PowerEntry
            {
                label        = "SMITE",
                description  = "Colpisci con un fulmine",
                power        = DivinePower.Smite,
                normalColor  = new Color(0.38f, 0.07f, 0.07f, 1f),
                selectedColor= new Color(0.90f, 0.15f, 0.10f, 1f),
            },
            new PowerEntry
            {
                label        = "REPAIR",
                description  = "Ripristina un oggetto",
                power        = DivinePower.Repair,
                normalColor  = new Color(0.07f, 0.28f, 0.10f, 1f),
                selectedColor= new Color(0.15f, 0.70f, 0.28f, 1f),
            },
            new PowerEntry
            {
                label        = "REVIVE",
                description  = "Riporta in vita un villager",
                power        = DivinePower.Revive,
                normalColor  = new Color(0.18f, 0.08f, 0.30f, 1f),
                selectedColor= new Color(0.55f, 0.20f, 0.90f, 1f),
            },
        };

        // Runtime
        private RectTransform  _panel;
        private Text           _toggleArrow;
        private bool           _isOpen;
        private List<Image>    _btnBgs  = new List<Image>();
        private DivinePower    _selected = DivinePower.None;

        // Colors
        private static readonly Color ColPanel  = new Color(0.07f, 0.07f, 0.12f, 0.93f);
        private static readonly Color ColToggle = new Color(0.16f, 0.16f, 0.24f, 1f);
        private static readonly Color ColTitle  = new Color(1.00f, 0.65f, 0.20f, 1f);
        private static readonly Color ColDesc   = new Color(0.70f, 0.70f, 0.72f, 1f);

        void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            BuildUI();
        }

        void Update()
        {
            if (_panel == null) return;

            // Slide: quando chiuso il toggle sporge a destra
            float targetX  = _isOpen ? 0f : -(panelWidth - toggleWidth);
            _panel.anchoredPosition = new Vector2(
                Mathf.Lerp(_panel.anchoredPosition.x, targetX, Time.deltaTime * slideSpeed),
                _panel.anchoredPosition.y);

            if (_toggleArrow != null)
                _toggleArrow.text = _isOpen ? "<" : ">";

            // Sync con DivineActionSystem
            if (divineAction != null && divineAction.PendingPower != _selected)
            {
                _selected = divineAction.PendingPower;
                RefreshColors();
            }
        }

        private void BuildUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            // ── Pannello root (lato sinistro) ─────────────────────────────
            GameObject panelGO = new GameObject("PowersPanel");
            panelGO.transform.SetParent(uiRoot, false);
            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(0f, 0.5f);
            _panel.anchorMax = new Vector2(0f, 0.5f);
            _panel.pivot     = new Vector2(0f, 0.5f);
            _panel.sizeDelta = new Vector2(panelWidth, 310f);
            _panel.anchoredPosition = new Vector2(-(panelWidth - toggleWidth), 0f);
            panelGO.AddComponent<Image>().color = ColPanel;

            // ── Toggle button (destra del pannello) ───────────────────────
            GameObject toggleGO = new GameObject("PowersToggle");
            toggleGO.transform.SetParent(_panel, false);
            RectTransform toggleRT = toggleGO.AddComponent<RectTransform>();
            toggleRT.anchorMin        = new Vector2(1f, 0.5f);
            toggleRT.anchorMax        = new Vector2(1f, 0.5f);
            toggleRT.pivot            = new Vector2(0f, 0.5f);
            toggleRT.sizeDelta        = new Vector2(toggleWidth, 64f);
            toggleRT.anchoredPosition = Vector2.zero;
            toggleGO.AddComponent<Image>().color = ColToggle;
            toggleGO.AddComponent<Button>().onClick.AddListener(() => _isOpen = !_isOpen);

            GameObject arrowGO = MakeText("Arrow", toggleGO.transform, font, ">", 13, TextAnchor.MiddleCenter, Color.white);
            var aRT = arrowGO.GetComponent<RectTransform>();
            aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
            aRT.offsetMin = aRT.offsetMax = Vector2.zero;
            _toggleArrow = arrowGO.GetComponent<Text>();

            // ── Titolo ────────────────────────────────────────────────────
            var titleGO = MakeText("Title", _panel, font, "POWERS", 12, TextAnchor.MiddleCenter, ColTitle);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0f, 1f);
            titleRT.anchorMax        = new Vector2(1f, 1f);
            titleRT.pivot            = new Vector2(0.5f, 1f);
            titleRT.sizeDelta        = new Vector2(0f, 32f);
            titleRT.anchoredPosition = new Vector2(0f, -4f);
            titleGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Separatore
            MakeDivider(_panel, -38f);

            // ── Bottoni ───────────────────────────────────────────────────
            float yOff = -46f;
            _btnBgs.Clear();
            foreach (var entry in _entries)
            {
                _btnBgs.Add(MakePowerButton(_panel, font, entry, yOff));
                yOff -= 82f;
            }
        }

        private Image MakePowerButton(Transform parent, Font font, PowerEntry entry, float yOff)
        {
            GameObject btnGO = new GameObject("PwrBtn_" + entry.power);
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(-10f, 72f);
            rt.anchoredPosition = new Vector2(0f, yOff);

            Image bg = btnGO.AddComponent<Image>();
            bg.color = entry.normalColor;

            // Label
            var lblGO = MakeText("Lbl", btnGO.transform, font, entry.label, 13, TextAnchor.UpperCenter, Color.white);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0.5f); lblRT.anchorMax = new Vector2(1f, 1f);
            lblRT.offsetMin = new Vector2(6f, 0f);   lblRT.offsetMax = new Vector2(-6f, -4f);
            lblGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Description
            var descGO = MakeText("Desc", btnGO.transform, font, entry.description, 10, TextAnchor.LowerCenter, ColDesc);
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0f); descRT.anchorMax = new Vector2(1f, 0.5f);
            descRT.offsetMin = new Vector2(6f, 4f);  descRT.offsetMax = new Vector2(-6f, 0f);

            // Button
            Button btn = btnGO.AddComponent<Button>();
            PowerEntry cap = entry;
            Image capBg = bg;
            btn.onClick.AddListener(() => OnPowerClicked(cap, capBg));

            return bg;
        }

        private void OnPowerClicked(PowerEntry entry, Image bg)
        {
            if (_selected == entry.power)
            {
                _selected = DivinePower.None;
                divineAction?.ClearPendingPower();
            }
            else
            {
                _selected = entry.power;
                divineAction?.SetPendingPower(entry.power);
            }
            RefreshColors();
        }

        private void RefreshColors()
        {
            for (int i = 0; i < _entries.Count && i < _btnBgs.Count; i++)
            {
                var e = _entries[i];
                _btnBgs[i].color = (_selected == e.power) ? e.selectedColor : e.normalColor;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private GameObject MakeText(string name, Transform parent, Font font, string text,
                                    int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.text = text; t.font = font; t.fontSize = size;
            t.alignment = anchor; t.color = color;
            return go;
        }

        private void MakeDivider(Transform parent, float yOff)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-14f, 1f);
            rt.anchoredPosition = new Vector2(0f, yOff);
            go.AddComponent<Image>().color = new Color(0.6f, 0.4f, 0.2f, 0.6f);
        }
    }
}
