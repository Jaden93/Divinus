using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace DivinePrototype
{
    /// <summary>
    /// Manager centrale per TUTTE le risorse del villaggio.
    /// Sostituisce WoodDepot e StoneDepot.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [System.Serializable]
        public class ResourceData
        {
            public string name;
            public int count;
            public int maxWithoutDepot;
            public int maxWithDepot;
            public int currentMax;
            public Color feedbackColor;
            public UnityEvent<int> onChanged;
        }

        [Header("Risorse Config")]
        public ResourceData wood = new ResourceData { 
            name = "Wood", 
            maxWithoutDepot = 9, 
            maxWithDepot = 30, 
            currentMax = 9,
            feedbackColor = new Color(0.9f, 0.7f, 0.2f)
        };

        public ResourceData stone = new ResourceData { 
            name = "Stone", 
            maxWithoutDepot = 3, 
            maxWithDepot = 20, 
            currentMax = 3,
            feedbackColor = new Color(0.6f, 0.6f, 0.6f)
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            RefreshCaps();
            wood.onChanged?.Invoke(wood.count);
            stone.onChanged?.Invoke(stone.count);
        }

        public void RefreshCaps()
        {
            bool hasPhysicalDepot = FindObjectOfType<GenericDepotController>() != null;
            
            wood.currentMax = hasPhysicalDepot ? wood.maxWithDepot : wood.maxWithoutDepot;
            stone.currentMax = hasPhysicalDepot ? stone.maxWithDepot : stone.maxWithoutDepot;
            
            Debug.Log($"[ResourceManager] Cap aggiornati. Wood: {wood.currentMax}, Stone: {stone.currentMax}");
        }

        public void AddResource(string type, int amount)
        {
            ResourceData data = GetResourceData(type);
            if (data == null) return;

            if (data.count >= data.currentMax)
            {
                Debug.Log($"[ResourceManager] Cap {data.name} raggiunto ({data.currentMax}).");
                return;
            }

            data.count = Mathf.Min(data.currentMax, data.count + amount);
            data.onChanged?.Invoke(data.count);

            // Feedback visivo
            var depot = FindNearestDepot(Vector3.zero); // In futuro usa pos villager
            Vector3 pos = depot != null ? depot.transform.position + Vector3.up : Vector3.up * 2f;
            
            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn($"+{amount} {data.name}", pos, data.feedbackColor);

            Debug.Log($"[ResourceManager] {data.name}: {data.count}/{data.currentMax}");
        }

        public bool SpendResource(string type, int amount)
        {
            ResourceData data = GetResourceData(type);
            if (data != null && data.count >= amount)
            {
                data.count -= amount;
                data.onChanged?.Invoke(data.count);
                return true;
            }
            return false;
        }

        /// <summary>Verifica se ci sono abbastanza risorse di entrambi i tipi.</summary>
        public bool HasResources(int woodAmount, int stoneAmount)
        {
            return wood.count >= woodAmount && stone.count >= stoneAmount;
        }

        public ResourceData GetResourceData(string type)
        {
            if (type.ToLower() == "wood") return wood;
            if (type.ToLower() == "stone") return stone;
            return null;
        }

        private GenericDepotController FindNearestDepot(Vector3 from)
        {
            GenericDepotController best = null;
            float minDist = float.MaxValue;
            foreach (var d in FindObjectsOfType<GenericDepotController>())
            {
                float dist = Vector3.Distance(from, d.transform.position);
                if (dist < minDist) { minDist = dist; best = d; }
            }
            return best;
        }
    }
}
