using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Mostra un solo obiettivo attivo alla volta nella parte bassa dello schermo.
    /// Avanza automaticamente ascoltando gli eventi di GameStateSystem e ResourceManager.
    /// </summary>
    public class ObjectiveHintUI : MonoBehaviour
    {
        [Header("Riferimenti")]
        public GameStateSystem gameState;
        public Text            hintText;

        private enum Objective { GetAxe, CollectResources, BuildHouse, WatchRest, Done }
        private Objective _current = Objective.GetAxe;

        private static readonly string[] _hints = new[]
        {
            "Get the axe — drag it onto the villager",
            "Collect wood and stone (6 wood, 3 stone)",
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
                gameState.onHouseBuilt.AddListener(OnHouseBuilt);
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.wood.onChanged.AddListener(OnResourcesChanged);
                ResourceManager.Instance.stone.onChanged.AddListener(OnResourcesChanged);
            }

            // Se il loop è già parzialmente avanzato, saltare gli step già fatti
            FastForwardToCurrentState();
            RefreshText();
        }

        private void OnDestroy()
        {
            if (gameState != null)
            {
                gameState.onAxeGranted.RemoveListener(OnAxeGranted);
                gameState.onHouseBuilt.RemoveListener(OnHouseBuilt);
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.wood.onChanged.RemoveListener(OnResourcesChanged);
                ResourceManager.Instance.stone.onChanged.RemoveListener(OnResourcesChanged);
            }
        }

        // ── Listeners ───────────────────────────────────────────────────

        private void OnAxeGranted()
        {
            if (_current == Objective.GetAxe) AdvanceTo(Objective.CollectResources);
        }

        private void OnResourcesChanged(int amount)
        {
            if (_current == Objective.CollectResources && ResourceManager.Instance != null)
            {
                if (ResourceManager.Instance.HasResources(6, 3))
                    AdvanceTo(Objective.BuildHouse);
            }
        }

        private void OnHouseBuilt()
        {
            if (_current == Objective.BuildHouse) AdvanceTo(Objective.WatchRest);
            Invoke(nameof(FinishRest), 15f);
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
            if (gameState.HasHouse) { _current = Objective.Done; return; }

            bool enoughResources = false;
            if (ResourceManager.Instance != null)
                enoughResources = ResourceManager.Instance.HasResources(6, 3);
            else
                enoughResources = gameState.WoodCount >= 6;

            if (enoughResources) { _current = Objective.BuildHouse; return; }
            if (gameState.HasAxe) { _current = Objective.CollectResources; return; }
        }
    }
}
