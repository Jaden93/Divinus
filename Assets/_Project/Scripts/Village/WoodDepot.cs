using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Proxy per ResourceManager per mantenere compatibilità con il vecchio sistema WoodDepot.
    /// </summary>
    public class WoodDepot : MonoBehaviour
    {
        public static WoodDepot Instance { get; private set; }

        public UnityEvent<int> onWoodDeposited;
        public UnityEvent      onConstructionReady;

        public int WoodCount => ResourceManager.Instance != null ? ResourceManager.Instance.wood.count : 0;
        public int MaxWood   => ResourceManager.Instance != null ? ResourceManager.Instance.wood.currentMax : 9;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.wood.onChanged.AddListener(val => onWoodDeposited?.Invoke(val));
            }
        }

        public void DepositWood(int amount) => ResourceManager.Instance?.AddResource("Wood", amount);
        public void ConsumeWood(int amount) => ResourceManager.Instance?.SpendResource("Wood", amount);
        public void SetMaxWood(int max)     => ResourceManager.Instance?.RefreshCaps();
    }
}
