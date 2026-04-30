using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// HUD principale risorse: top-left, icone permanenti per Wood e Stone
    /// con valori live da ResourceManager.
    /// Costruito a runtime: basta posarlo su un Canvas in scena.
    /// </summary>
    public class MainResourceHUD : MonoBehaviour
    {
        [Header("Sprites (opzionali — fallback quadrato colorato)")]
        public Sprite woodIcon;
        public Sprite stoneIcon;

        [Header("Layout")]
        public Vector2 anchoredPosition = new Vector2(20f, -20f);
        public Vector2 panelSize        = new Vector2(220f, 110f);
        public float   iconSize         = 40f;
        public int     fontSize         = 22;

        [Header("Colori fallback icone (se sprite null)")]
        public Color woodFallbackColor  = new Color(0.78f, 0.55f, 0.20f);
        public Color stoneFallbackColor = new Color(0.55f, 0.55f, 0.55f);

        [Header("Format valore")]
        [Tooltip("True = mostra count/max, False = solo count.")]
        public bool showMax = true;

        private Text _woodValue;
        private Text _stoneValue;
        private Font _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[MainResourceHUD] Nessun Canvas trovato in scena.");
                return;
            }

            // Container top-left
            var panel = new GameObject("MainResourceHUD_Panel");
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta        = panelSize;

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding              = new RectOffset(8, 8, 8, 8);
            layout.spacing              = 6f;
            layout.childAlignment       = TextAnchor.MiddleLeft;
            layout.childControlWidth    = true;
            layout.childControlHeight   = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _woodValue  = MakeRow(panel.transform, "WoodRow",  woodIcon,  woodFallbackColor);
            _stoneValue = MakeRow(panel.transform, "StoneRow", stoneIcon, stoneFallbackColor);
        }

        private Text MakeRow(Transform parent, string name, Sprite icon, Color fallback)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.spacing               = 10f;
            hl.childAlignment        = TextAnchor.MiddleLeft;
            hl.childControlWidth     = true;
            hl.childControlHeight    = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = iconSize;

            // Icona
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(row.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(iconSize, iconSize);
            var iconImg = iconGO.AddComponent<Image>();
            if (icon != null)
            {
                iconImg.sprite = icon;
                iconImg.color  = Color.white;
            }
            else
            {
                iconImg.color = fallback;
            }
            iconImg.preserveAspect = true;
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = iconSize;
            iconLE.preferredHeight = iconSize;

            // Valore
            var txtGO = new GameObject("ValueText");
            txtGO.transform.SetParent(row.transform, false);
            txtGO.AddComponent<RectTransform>();
            var txt = txtGO.AddComponent<Text>();
            txt.text      = "0";
            txt.alignment = TextAnchor.MiddleLeft;
            txt.fontSize  = fontSize;
            txt.color     = Color.white;
            if (_font != null) txt.font = _font;

            return txt;
        }

        private void Update()
        {
            if (ResourceManager.Instance == null) return;

            if (_woodValue != null)
                _woodValue.text = Format(ResourceManager.Instance.wood);

            if (_stoneValue != null)
                _stoneValue.text = Format(ResourceManager.Instance.stone);
        }

        private string Format(ResourceManager.ResourceData data)
        {
            return showMax ? $"{data.count}/{data.currentMax}" : data.count.ToString();
        }
    }
}
