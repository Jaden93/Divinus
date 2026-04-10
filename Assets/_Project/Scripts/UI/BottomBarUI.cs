using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Barra inferiore con due categorie mutuamente esclusive.
    /// Gestisce apertura/chiusura dei menu circolari e
    /// collega la selezione degli item alle modalità di piazzamento.
    /// </summary>
    public class BottomBarUI : MonoBehaviour
    {
        [Header("Pulsanti categoria")]
        public Button toolsButton;
        public Button lodgingsButton;
        public Button divineButton;

        [Header("Menu circolari (figli di HUD_Canvas)")]
        public CircularMenuUI toolsMenu;
        public CircularMenuUI lodgingsMenu;
        public CircularMenuUI divineMenu;

        [Header("Background pulsanti")]
        public Image toolsBtnBg;
        public Image lodgingsBtnBg;
        public Image divineBtnBg;

        [Header("Colori")]
        public Color colorActive   = new Color(1.00f, 0.82f, 0.18f, 1.00f);
        public Color colorInactive = new Color(0.12f, 0.12f, 0.18f, 0.88f);

        [Header("ActionUI da attivare")]
        public AxeActionUI        axeActionUI;
        public HouseActionUI      houseActionUI;
        public BenchActionUI      benchActionUI;
        public WoodDepotActionUI  woodDepotActionUI;
        public VillagerActionUI   villagerActionUI;
        public VillagerActionUI   dogActionUI; // Placeholder
        public VillagerActionUI   catActionUI; // Placeholder

        private void Start()
        {
            if (toolsButton    != null) toolsButton.onClick.AddListener(OnToolsClick);
            if (lodgingsButton != null) lodgingsButton.onClick.AddListener(OnLodgingsClick);
            if (divineButton   != null) divineButton.onClick.AddListener(OnDivineClick);

            // Collega eventi item selezionato
            if (toolsMenu    != null) toolsMenu.onItemSelected.AddListener(OnToolsItemSelected);
            if (lodgingsMenu != null) lodgingsMenu.onItemSelected.AddListener(OnLodgingsItemSelected);
            if (divineMenu   != null) divineMenu.onItemSelected.AddListener(OnDivineItemSelected);

            RefreshColors();
        }

        // ── Click sui bottoni categoria ───────────────────────────────────

        private void OnToolsClick()
        {
            bool willOpen = !toolsMenu.IsOpen;
            lodgingsMenu.CloseMenu();
            if (divineMenu != null) divineMenu.CloseMenu();
            if (willOpen) toolsMenu.OpenMenu();
            else          toolsMenu.CloseMenu();
            RefreshColors();
        }

        private void OnLodgingsClick()
        {
            bool willOpen = !lodgingsMenu.IsOpen;
            toolsMenu.CloseMenu();
            if (divineMenu != null) divineMenu.CloseMenu();
            if (willOpen) lodgingsMenu.OpenMenu();
            else          lodgingsMenu.CloseMenu();
            RefreshColors();
        }

        private void OnDivineClick()
        {
            if (divineMenu == null) return;
            bool willOpen = !divineMenu.IsOpen;
            toolsMenu.CloseMenu();
            lodgingsMenu.CloseMenu();
            if (willOpen) divineMenu.OpenMenu();
            else          divineMenu.CloseMenu();
            RefreshColors();
        }

        // ── Selezione item dal menu ───────────────────────────────────────

        /// <summary>
        /// Tools menu: index 0 = Axe
        /// </summary>
        private void OnToolsItemSelected(int index)
        {
            RefreshColors();
            if (index == 0 && axeActionUI != null)
                axeActionUI.StartAssignmentMode();
        }

        /// <summary>
        /// Lodgings menu: index 0 = House, index 1 = Bench
        /// </summary>
        private void OnLodgingsItemSelected(int index)
        {
            RefreshColors();
            if (index == 0 && houseActionUI != null)
                houseActionUI.StartPlacementMode();
            else if (index == 1 && benchActionUI != null)
                benchActionUI.StartPlacementMode();
            else if (index == 2 && woodDepotActionUI != null)
                woodDepotActionUI.StartPlacementMode();
        }

        /// <summary>
        /// Divine menu: index 0 = Villager, index 1 = Dog, index 2 = Cat
        /// </summary>
        private void OnDivineItemSelected(int index)
        {
            RefreshColors();
            if (index == 0 && villagerActionUI != null)
                villagerActionUI.StartPlacementMode();
            else if (index == 1 && dogActionUI != null)
                dogActionUI.StartPlacementMode();
            else if (index == 2 && catActionUI != null)
                catActionUI.StartPlacementMode();
        }

        // ── API pubblica ──────────────────────────────────────────────────

        public void CloseAll()
        {
            toolsMenu.CloseMenu();
            lodgingsMenu.CloseMenu();
            if (divineMenu != null) divineMenu.CloseMenu();
            RefreshColors();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void RefreshColors()
        {
            if (toolsBtnBg    != null) toolsBtnBg.color    = (toolsMenu    != null && toolsMenu.IsOpen)    ? colorActive : colorInactive;
            if (lodgingsBtnBg != null) lodgingsBtnBg.color = (lodgingsMenu != null && lodgingsMenu.IsOpen) ? colorActive : colorInactive;
            if (divineBtnBg   != null) divineBtnBg.color   = (divineMenu   != null && divineMenu.IsOpen)   ? colorActive : colorInactive;
        }
    }
}
