using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Classe base per qualsiasi risorsa raccoglibile (alberi, rocce, ecc.).
    /// Gestisce la quantità, l'assegnazione e il prelievo parziale.
    /// </summary>
    public abstract class ResourceNode : MonoBehaviour
    {
        public enum NodeState { Intact, BeingChopped, Depleted }

        [Header("Resource Settings")]
        public string resourceName = "Wood";
        public int amount = 3;
        
        [Header("Divine Smite Visuals")]
        public GameObject resourceCubePrefab; 

        [Header("Events")]
        public UnityEvent<ResourceNode> onDepleted = new UnityEvent<ResourceNode>();

        public NodeState State { get; protected set; } = NodeState.Intact;
        protected VillagerController assignedVillager;

        protected virtual void Awake()
        {
            // Aggiunge ostacolo NavMesh per evitare che i villager passino attraverso il modello
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            // Impostiamo una dimensione standard se non rileva collider
            var col = GetComponent<Collider>();
            if (col != null) obstacle.size = col.bounds.size;
            else obstacle.size = Vector3.one * 2f;
        }

        public virtual bool TryAssign(VillagerController villager)
        {
            if (State != NodeState.Intact || amount <= 0) return false;
            assignedVillager = villager;
            State = NodeState.BeingChopped;
            return true;
        }

        public virtual void Release()
        {
            if (State == NodeState.BeingChopped)
                State = NodeState.Intact;
            assignedVillager = null;
        }

        public virtual int TakeResource(int requestedAmount)
        {
            int taken = Mathf.Min(requestedAmount, amount);
            amount -= taken;
            if (amount <= 0) Deplete();
            else Release();
            return taken;
        }

        public virtual void Deplete()
        {
            if (State == NodeState.Depleted) return;
            State = NodeState.Depleted;
            onDepleted.Invoke(this);
            OnDepleteVisuals();
        }

        /// <summary>
        /// Forza il depletamento immediato (usato dallo Smite) spawnando cubi fisici.
        /// </summary>
        public virtual void SmiteDeplete()
        {
            if (State == NodeState.Depleted) return;

            Debug.Log($"[ResourceNode] SmiteDeplete su {name}. Cubi da spawnare: {amount}");

            int cubesToSpawn = amount;
            if (cubesToSpawn <= 0) cubesToSpawn = 1;

            for (int i = 0; i < cubesToSpawn; i++)
            {
                // Spawna leggermente più in alto per sicurezza
                Vector3 spawnPos = transform.position + Vector3.up * 2.0f + Random.insideUnitSphere * 0.5f;
                GameObject cube;

                if (resourceCubePrefab != null)
                {
                    cube = Instantiate(resourceCubePrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    // FALLBACK: Crea un cubo primitivo SOLIDO (non trigger)
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = spawnPos;
                    cube.transform.localScale = Vector3.one * 0.4f;
                    
                    var rb = cube.AddComponent<Rigidbody>();
                    rb.mass = 0.5f;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Più preciso per oggetti piccoli
                    
                    // Il BoxCollider creato da CreatePrimitive è già NON-TRIGGER (solido)
                    
                    var rend = cube.GetComponent<Renderer>();
                    if (rend != null) rend.material.color = (resourceName == "Stone") ? Color.gray : new Color(0.5f, 0.25f, 0f);
                }

                var pickup = cube.GetComponent<ResourcePickup>();
                if (pickup == null) pickup = cube.AddComponent<ResourcePickup>();
                
                pickup.Initialize(resourceName, 1);
            }

            Deplete();
        }

        protected abstract void OnDepleteVisuals();
    }
}
