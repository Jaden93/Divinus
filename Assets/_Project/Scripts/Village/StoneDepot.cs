using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Proxy per ResourceManager per mantenere compatibilità con il vecchio sistema StoneDepot.
    /// </summary>
    public class StoneDepot : MonoBehaviour
    {
        public static StoneDepot Instance { get; private set; }

        public UnityEvent<int> onStoneChanged;
        public UnityEvent      onConstructionReady;

        public int StoneCount => ResourceManager.Instance != null ? ResourceManager.Instance.stone.count : 0;
        public int MaxStone   => ResourceManager.Instance != null ? ResourceManager.Instance.stone.currentMax : 3;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.stone.onChanged.AddListener(val => onStoneChanged?.Invoke(val));
            }
        }

        public void AddStone(int amount)   => ResourceManager.Instance?.AddResource("Stone", amount);
        public void SpendStone(int amount) => ResourceManager.Instance?.SpendResource("Stone", amount);
        public void SetMaxStone(int max)   => ResourceManager.Instance?.RefreshCaps();
    }
}
