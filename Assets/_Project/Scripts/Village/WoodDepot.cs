using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Contatore globale della legna. Singleton.
    /// Cap: 9 senza depot fisico, 30 con depot fisico (WoodDepotController).
    /// </summary>
    public class WoodDepot : MonoBehaviour
    {
        public const int MAX_WOOD_WITHOUT_DEPOT = 9;
        public const int MAX_WOOD_WITH_DEPOT    = 30;

        public static WoodDepot Instance { get; private set; }

        [Header("Setup")]
        public int woodToStartConstruction = 6;

        [Header("Events")]
        public UnityEvent<int> onWoodDeposited;
        public UnityEvent onConstructionReady;

        public int WoodCount          { get; private set; } = 0;
        public int MaxWood            { get; private set; } = MAX_WOOD_WITHOUT_DEPOT;
        public bool IsConstructionReady { get; private set; } = false;

        [Header("Riferimenti")]
        public GameStateSystem gameState;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();
        }

        public void SetMaxWood(int max)
        {
            MaxWood = max;
            Debug.Log($"[WoodDepot] Cap legna aggiornato: {MaxWood}");
        }

        /// <summary>Consuma legna per una costruzione.</summary>
        public void ConsumeWood(int amount)
        {
            WoodCount = Mathf.Max(0, WoodCount - amount);
            if (gameState != null) gameState.WoodCount = WoodCount;
            IsConstructionReady = false;
            Debug.Log($"[WoodDepot] Legna consumata. Rimasta: {WoodCount}");
        }

        public void DepositWood(int amount)
        {
            if (WoodCount >= MaxWood)
            {
                Debug.Log($"[WoodDepot] Cap raggiunto ({MaxWood}). Legna ignorata.");
                return;
            }

            WoodCount = Mathf.Min(MaxWood, WoodCount + amount);
            if (gameState != null) gameState.WoodCount = WoodCount;
            onWoodDeposited?.Invoke(WoodCount);

            // Posizione feedback: usa il depot fisico più vicino se esiste, altrimenti origin
            Vector3 feedbackPos = Vector3.up * 1f;
            var ctrl = FindObjectOfType<WoodDepotController>();
            if (ctrl != null) feedbackPos = ctrl.transform.position + Vector3.up;

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn($"+{amount} wood", feedbackPos, new Color(0.9f, 0.7f, 0.2f));

            Debug.Log($"[WoodDepot] Legna: {WoodCount}/{MaxWood}");

            if (!IsConstructionReady && WoodCount >= woodToStartConstruction)
            {
                IsConstructionReady = true;
                onConstructionReady?.Invoke();
                Debug.Log("[WoodDepot] Legna sufficiente per costruire!");
            }
        }
    }
}
