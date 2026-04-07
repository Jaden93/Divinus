using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Mostra un solo obiettivo attivo alla volta nella parte bassa dello schermo.
    /// Avanza automaticamente ascoltando gli eventi di GameStateSystem.
    /// </summary>
    public class ObjectiveHintUI : MonoBehaviour
    {
        [Header("Riferimenti")]
        public GameStateSystem gameState;
        public Text            hintText;

        private enum Objective { GetAxe, CollectWood, BuildHouse, WatchRest, Done }
        private Objective _current = Objective.GetAxe;

        private static readonly string[] _hints = new[]
        {
            "Get the axe — drag it onto the villager",
            "Collect enough wood (6 logs)",
            "Build the first house — drag HOME 1 onto the ground",
            "Watch the villager rest in the house",
            ""
        };

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();

            if (gameState != null)
            {
                gameState.onAxeGranted.AddListener(OnAxeGranted);
                gameState.onWoodChanged.AddListener(OnWoodChanged);
                gameState.onHouseBuilt.AddListener(OnHouseBuilt);
            }

            // Se il loop è già parzialmente avanzato (es. reset parziale), saltare gli step già fatti
            FastForwardToCurrentState();
            RefreshText();
        }

        private void OnDestroy()
        {
            if (gameState == null) return;
            gameState.onAxeGranted.RemoveListener(OnAxeGranted);
            gameState.onWoodChanged.RemoveListener(OnWoodChanged);
            gameState.onHouseBuilt.RemoveListener(OnHouseBuilt);
        }

        // ── Listeners ───────────────────────────────────────────────────

        private void OnAxeGranted()
        {
            if (_current == Objective.GetAxe) AdvanceTo(Objective.CollectWood);
        }

        private void OnWoodChanged(int amount)
        {
            if (_current == Objective.CollectWood && amount >= 6)
                AdvanceTo(Objective.BuildHouse);
        }

        private void OnHouseBuilt()
        {
            if (_current == Objective.BuildHouse) AdvanceTo(Objective.WatchRest);
            // "WatchRest" si chiude quando il villager si sveglia — per ora lo lasciamo
            // attivo finché non viene implementato un evento di wake-up
            Invoke(nameof(FinishRest), 15f); // 8s sleep + 7s margine
        }

        private void FinishRest()
        {
            if (_current == Objective.WatchRest) AdvanceTo(Objective.Done);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void AdvanceTo(Objective next)
        {
            _current = next;
            RefreshText();
        }

        private void RefreshText()
        {
            if (hintText == null) return;
            hintText.text = _hints[(int)_current];
        }

        private void FastForwardToCurrentState()
        {
            if (gameState == null) return;
            if (gameState.HasHouse)          { _current = Objective.Done;        return; }
            if (gameState.WoodCount >= 6)    { _current = Objective.BuildHouse;  return; }
            if (gameState.HasAxe)            { _current = Objective.CollectWood; return; }
        }
    }
}
