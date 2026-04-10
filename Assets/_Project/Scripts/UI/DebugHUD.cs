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
            Debug.Log("[DebugHUD] Start");
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();
            _constructionSite = FindObjectOfType<ConstructionSite>();
            CreateEnergyButtons();
        }

        private void CreateEnergyButtons()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                Debug.LogError("[DebugHUD] Nessun Canvas trovato in scena! I tasti di debug non verranno creati.");
                return;
            }

            Debug.Log($"[DebugHUD] Creazione tasti su Canvas: {canvas.name}");

            // Container bottom-left (spostato un po' più su per evitare la barra di navigazione Android/iOS)
            var container = new GameObject("EnergyDebugButtons");
            container.transform.SetParent(canvas.transform, false);
            var crt = container.AddComponent<RectTransform>();
            crt.anchorMin        = new Vector2(0f, 0f);
            crt.anchorMax        = new Vector2(0f, 0f);
            crt.pivot            = new Vector2(0f, 0f);
            crt.anchoredPosition = new Vector2(20f, 150f); // Alzato ancora un po'
            crt.sizeDelta        = new Vector2(400f, 70f);

            // Aggiungi un'immagine di sfondo semitrasparente per vedere il container
            var bg = container.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);

            var layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing          = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = true;

            // Forza il container in primo piano
            container.transform.SetAsLastSibling();

            MakeButton(container.transform, "Social", new Color(1f, 0.5f, 0f), () =>
            {
                RefreshVillager();
                if (_villager != null)
                {
                    DivineEventManager.Broadcast(new DivineEvent {
                        Type     = DivineEventType.Smite,
                        Position = _villager.transform.position,
                        Target   = _villager.gameObject,
                        Radius   = 20f
                    });
                    Debug.Log("[DebugHUD] Triggered Social Test (Smite) at villager position.");
                }
                else
                {
                    Debug.LogWarning("[DebugHUD] No villager found to test social reaction.");
                }
            });

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
            rt.sizeDelta = new Vector2(100f, 60f);

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
            
            // Usa il font legacy consigliato da Unity per evitare eccezioni
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
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

            string wood  = "?";
            string stone = "?";

            if (ResourceManager.Instance != null)
            {
                wood  = ResourceManager.Instance.wood.count.ToString();
                stone = ResourceManager.Instance.stone.count.ToString();
            }

            string axe       = gameState != null && gameState.HasAxe ? "YES" : "NO";
            string villState = _villager != null ? _villager.CurrentState.ToString() : "—";
            string energy    = _villager != null ? _villager.Energy.ToString("0") : "—";
            string house     = GetHouseStatus();

            hudText.text = $"WOOD: {wood}\nSTONE: {stone}\nVILLAGER: {villState}\nENERGY: {energy}\nAXE: {axe}\nHOUSE: {house}";
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
