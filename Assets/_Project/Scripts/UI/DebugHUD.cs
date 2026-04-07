using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// HUD debug top-left: wood, villager state, axe, house status.
    /// Auto-trova i riferimenti a runtime (VillagerController spawna dinamicamente).
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [Header("Riferimenti (auto-trovati se null)")]
        public GameStateSystem gameState;
        public Text            hudText;

        private VillagerController  _villager;
        private ConstructionSite    _constructionSite;

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();
            _constructionSite = FindObjectOfType<ConstructionSite>();
            CreateEnergyButtons();
        }

        private void CreateEnergyButtons()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Container bottom-left
            var container = new GameObject("EnergyDebugButtons");
            container.transform.SetParent(canvas.transform, false);
            var crt = container.AddComponent<RectTransform>();
            crt.anchorMin        = new Vector2(0f, 0f);
            crt.anchorMax        = new Vector2(0f, 0f);
            crt.pivot            = new Vector2(0f, 0f);
            crt.anchoredPosition = new Vector2(10f, 10f);
            crt.sizeDelta        = new Vector2(180f, 50f);

            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing          = 8f;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = true;

            MakeButton(container.transform, "Drain", new Color(0.8f, 0.2f, 0.2f), () =>
            {
                RefreshVillager();
                if (_villager != null) _villager.SetEnergy(10f);
            });

            MakeButton(container.transform, "Fill", new Color(0.2f, 0.7f, 0.2f), () =>
            {
                RefreshVillager();
                if (_villager != null) _villager.SetEnergy(_villager.maxEnergy);
            });
        }

        private void MakeButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Btn");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(86f, 50f);

            go.AddComponent<CanvasRenderer>();
            var img   = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            textGO.AddComponent<CanvasRenderer>();
            var txt       = textGO.AddComponent<Text>();
            txt.text      = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 20;
            txt.color     = Color.white;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) txt.font = f;
        }

        private void RefreshVillager()
        {
            if (_villager == null) _villager = FindObjectOfType<VillagerController>();
        }

        private void Update()
        {
            RefreshVillager();
            if (hudText == null) return;

            string wood      = gameState != null ? gameState.WoodCount.ToString() : "?";
            string axe       = gameState != null && gameState.HasAxe ? "YES" : "NO";
            string villState = _villager != null ? _villager.CurrentState.ToString() : "—";
            string energy    = _villager != null ? _villager.Energy.ToString("0") : "—";
            string house     = GetHouseStatus();

            hudText.text = $"WOOD: {wood}\nVILLAGER: {villState}\nENERGY: {energy}\nAXE: {axe}\nHOUSE: {house}";
        }

        private string GetHouseStatus()
        {
            if (gameState == null) return "?";
            if (gameState.HasHouse)      return "COMPLETED";
            if (_constructionSite != null && _constructionSite.IsBuilding) return "BUILDING";
            return "LOCKED";
        }
    }
}
