using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Pannello laterale destro con i poteri divini disponibili.
    /// Si apre/chiude con un pulsante freccia. I poteri costano fede.
    /// Costruisce la propria UI via codice al momento dello Start.
    /// </summary>
    public class DivinePowerMenuUI : MonoBehaviour
    {
        [Header("Riferimenti (auto-trovati se null)")]
        public DivineActionSystem divineAction;
        public VillageFaithSystem faithSystem;

        [Header("Layout")]
        public float panelWidth   = 150f;
        public float toggleWidth  = 28f;
        public float slideSpeed   = 10f;

        // Definizione interna dei poteri
        private class PowerEntry
        {
            public string        label;
            public DivinePower   power;
            public float         faithCost;
            public bool          isLocked;
        }

        private readonly List<PowerEntry> _entries = new List<PowerEntry>
        {
            new PowerEntry { label = "Uomo",  power = DivinePower.SpawnMale,   faithCost = 0f, isLocked = false },
            new PowerEntry { label = "Donna", power = DivinePower.SpawnFemale, faithCost = 0f, isLocked = false },
            new PowerEntry { label = "Messaggero", power = DivinePower.Messenger, faithCost = 0f, isLocked = false },
            new PowerEntry { label = "Cane",  power = DivinePower.SpawnDog,    faithCost = 0f, isLocked = false  },
            new PowerEntry { label = "Gatto", power = DivinePower.SpawnCat,    faithCost = 0f, isLocked = false  },
        };

        // Stato runtime
        private RectTransform        _panel;
        private Text                 _toggleArrow;
        private bool                 _isOpen = false;
        private List<Image>          _btnBgs  = new List<Image>();
        private DivinePower          _localSelected = DivinePower.None;

        // Colori
        private static readonly Color ColPanelBg    = new Color(0.07f, 0.07f, 0.12f, 0.93f);
        private static readonly Color ColBtnNormal   = new Color(0.20f, 0.26f, 0.36f, 1f);
        private static readonly Color ColBtnSelected = new Color(0.75f, 0.60f, 0.10f, 1f);
        private static readonly Color ColBtnLocked   = new Color(0.13f, 0.13f, 0.16f, 1f);
        private static readonly Color ColToggle      = new Color(0.16f, 0.16f, 0.24f, 1f);
        private static readonly Color ColGold        = new Color(1.00f, 0.85f, 0.30f, 1f);
        private static readonly Color ColGray        = new Color(0.40f, 0.40f, 0.42f, 1f);
        private static readonly Color ColSmite       = new Color(0.40f, 0.08f, 0.08f, 1f);
        private static readonly Color ColRepair      = new Color(0.08f, 0.32f, 0.12f, 1f);
        private static readonly Color ColSmiteSel    = new Color(0.85f, 0.15f, 0.10f, 1f);
        private static readonly Color ColRepairSel   = new Color(0.15f, 0.65f, 0.25f, 1f);

        private void Start()
        {
            if (divineAction == null) divineAction = FindObjectOfType<DivineActionSystem>();
            if (faithSystem  == null) faithSystem  = FindObjectOfType<VillageFaithSystem>();
            BuildUI();
        }

        private void Update()
        {
            if (_panel == null) return;

            // Slide panel — quando chiuso lascia sporgere il toggle button
            float targetX  = _isOpen ? 0f : panelWidth - toggleWidth;
            float currentX = _panel.anchoredPosition.x;
            _panel.anchoredPosition = new Vector2(
                Mathf.Lerp(currentX, targetX, Time.deltaTime * slideSpeed),
                _panel.anchoredPosition.y
            );

            // Freccia toggle
            if (_toggleArrow != null)
                _toggleArrow.text = _isOpen ? ">" : "<";

            // Sync selezione con DivineActionSystem (es. dopo spawn il potere viene azzerato)
            if (divineAction != null && divineAction.PendingPower != _localSelected)
            {
                _localSelected = divineAction.PendingPower;
                RefreshButtonColors();
            }
        }

        // ── Build UI ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Parentare al Canvas direttamente — questo GameObject potrebbe avere
            // un Transform normale invece di RectTransform se creato da MCP.
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform uiRoot = canvas != null ? canvas.transform : transform;

            // ── Pannello root ─────────────────────────────────────────────
            GameObject panelGO = new GameObject("DivinePowerPanel");
            panelGO.transform.SetParent(uiRoot, false);
            _panel = panelGO.AddComponent<RectTransform>();
            _panel.anchorMin = new Vector2(1f, 0.5f);
            _panel.anchorMax = new Vector2(1f, 0.5f);
            _panel.pivot     = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(panelWidth, 320f);
            _panel.anchoredPosition = new Vector2(panelWidth - toggleWidth, 0f); // nascosto ma toggle visibile

            panelGO.AddComponent<Image>().color = ColPanelBg;

            // ── Pulsante toggle (sinistra del pannello) ───────────────────
            GameObject toggleGO = new GameObject("ToggleBtn");
            toggleGO.transform.SetParent(_panel, false);
            RectTransform toggleRT = toggleGO.AddComponent<RectTransform>();
            toggleRT.anchorMin       = new Vector2(0f, 0.5f);
            toggleRT.anchorMax       = new Vector2(0f, 0.5f);
            toggleRT.pivot           = new Vector2(1f, 0.5f);
            toggleRT.sizeDelta       = new Vector2(toggleWidth, 64f);
            toggleRT.anchoredPosition = Vector2.zero;
            toggleGO.AddComponent<Image>().color = ColToggle;
            Button toggleBtn = toggleGO.AddComponent<Button>();
            toggleBtn.onClick.AddListener(() => _isOpen = !_isOpen);

            GameObject arrowGO = CreateTextGO("Arrow", toggleGO.transform, font, "<", 13, TextAnchor.MiddleCenter, Color.white);
            RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
            arrowRT.anchorMin = Vector2.zero;
            arrowRT.anchorMax = Vector2.one;
            arrowRT.offsetMin = arrowRT.offsetMax = Vector2.zero;
            _toggleArrow = arrowGO.GetComponent<Text>();

            // ── Titolo ────────────────────────────────────────────────────
            GameObject titleGO = CreateTextGO("Title", _panel, font, "POTERI DIVINI", 11, TextAnchor.MiddleCenter, ColGold);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin       = new Vector2(0f, 1f);
            titleRT.anchorMax       = new Vector2(1f, 1f);
            titleRT.pivot           = new Vector2(0.5f, 1f);
            titleRT.sizeDelta       = new Vector2(0f, 32f);
            titleRT.anchoredPosition = new Vector2(0f, -4f);
            titleGO.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // ── Separatore ────────────────────────────────────────────────
            CreateDivider(_panel, -38f);

            // ── Bottoni poteri ────────────────────────────────────────────
            float yOffset = -46f;
            _btnBgs.Clear();
            foreach (var entry in _entries)
            {
                Image bg = CreatePowerButton(_panel, font, entry, yOffset);
                _btnBgs.Add(bg);
                yOffset -= 66f;
            }
        }

        private Image CreatePowerButton(Transform parent, Font font, PowerEntry entry, float yOffset)
        {
            GameObject btnGO = new GameObject($"Btn_{entry.power}");
            btnGO.transform.SetParent(parent, false);
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0f, 1f);
            rt.anchorMax       = new Vector2(1f, 1f);
            rt.pivot           = new Vector2(0.5f, 1f);
            rt.sizeDelta       = new Vector2(-12f, 58f);
            rt.anchoredPosition = new Vector2(0f, yOffset);

            Image bg = btnGO.AddComponent<Image>();
            if (entry.isLocked)
                bg.color = ColBtnLocked;
            else if (entry.power == DivinePower.Smite)
                bg.color = ColSmite;
            else if (entry.power == DivinePower.Repair)
                bg.color = ColRepair;
            else
                bg.color = ColBtnNormal;

            // Label (nome)
            Color labelColor = entry.isLocked ? ColGray : Color.white;
            GameObject labelGO = CreateTextGO("Label", btnGO.transform, font, entry.label, 14, TextAnchor.UpperLeft, labelColor);
            RectTransform labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin       = new Vector2(0f, 0.5f);
            labelRT.anchorMax       = new Vector2(1f, 1f);
            labelRT.offsetMin       = new Vector2(10f, 0f);
            labelRT.offsetMax       = new Vector2(-6f, -4f);
            labelGO.GetComponent<Text>().fontStyle = entry.isLocked ? FontStyle.Normal : FontStyle.Bold;

            // Costo / stato lock
            string costLabel;
            Color  costColor;
            if (entry.isLocked)
            { costLabel = "[Bloccato]"; costColor = ColGray; }
            else if (entry.power == DivinePower.Smite)
            { costLabel = "fede -5"; costColor = new Color(1f, 0.4f, 0.4f); }
            else if (entry.power == DivinePower.Repair)
            { costLabel = "fede +3"; costColor = new Color(0.4f, 1f, 0.5f); }
            else
            { costLabel = $"{entry.faithCost} fede"; costColor = ColGold; }
            GameObject costGO = CreateTextGO("Cost", btnGO.transform, font, costLabel, 11, TextAnchor.LowerLeft, costColor);
            RectTransform costRT = costGO.GetComponent<RectTransform>();
            costRT.anchorMin       = new Vector2(0f, 0f);
            costRT.anchorMax       = new Vector2(1f, 0.5f);
            costRT.offsetMin       = new Vector2(10f, 4f);
            costRT.offsetMax       = new Vector2(-6f, 0f);

            // Button (solo se non bloccato)
            if (!entry.isLocked)
            {
                Button btn = btnGO.AddComponent<Button>();
                ColorBlock cb = btn.colors;
                cb.highlightedColor = new Color(0.30f, 0.38f, 0.52f);
                cb.pressedColor     = new Color(0.15f, 0.20f, 0.30f);
                btn.colors = cb;
                PowerEntry captured = entry;
                btn.onClick.AddListener(() => OnPowerClicked(captured, bg));
            }

            return bg;
        }

        // ── Logica selezione ─────────────────────────────────────────────

        private void OnPowerClicked(PowerEntry entry, Image bg)
        {
            if (_localSelected == entry.power)
            {
                // Secondo click = deseleziona
                _localSelected = DivinePower.None;
                divineAction?.ClearPendingPower();
            }
            else
            {
                _localSelected = entry.power;
                divineAction?.SetPendingPower(entry.power);
            }
            RefreshButtonColors();
        }

        private void RefreshButtonColors()
        {
            for (int i = 0; i < _entries.Count && i < _btnBgs.Count; i++)
            {
                var entry = _entries[i];
                if (entry.isLocked) continue;

                bool selected = (_localSelected == entry.power);

                if (entry.power == DivinePower.Smite)
                    _btnBgs[i].color = selected ? ColSmiteSel  : ColSmite;
                else if (entry.power == DivinePower.Repair)
                    _btnBgs[i].color = selected ? ColRepairSel : ColRepair;
                else
                    _btnBgs[i].color = selected ? ColBtnSelected : ColBtnNormal;
            }
        }

        // ── Helpers UI ───────────────────────────────────────────────────

        private GameObject CreateTextGO(string name, Transform parent, Font font, string content,
                                        int fontSize, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Text t = go.AddComponent<Text>();
            t.text      = content;
            t.font      = font;
            t.fontSize  = fontSize;
            t.alignment = anchor;
            t.color     = color;
            return go;
        }

        private void CreateDivider(Transform parent, float yOffset)
        {
            GameObject divGO = new GameObject("Divider");
            divGO.transform.SetParent(parent, false);
            RectTransform rt = divGO.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0f, 1f);
            rt.anchorMax       = new Vector2(1f, 1f);
            rt.pivot           = new Vector2(0.5f, 1f);
            rt.sizeDelta       = new Vector2(-16f, 1f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            Image img = divGO.AddComponent<Image>();
            img.color = new Color(0.4f, 0.4f, 0.5f, 0.5f);
        }
    }
}
