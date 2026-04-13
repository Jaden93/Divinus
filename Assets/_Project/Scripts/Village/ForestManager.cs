using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Registry dei ForestNode in scena.
    /// Ogni tick cerca villager idle con ascia e assegna automaticamente
    /// il task di taglio legna o RACCOLTA CUBI al nodo/pickup più vicino.
    /// </summary>
    public class ForestManager : MonoBehaviour
    {
        [Header("Riferimenti")]
        public GameStateSystem gameState;

        [Header("Tick assegnazione")]
        public float assignTickInterval = 2f;

        private readonly List<ForestNode> _nodes = new List<ForestNode>();
        private bool _stopped = false;

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();
            if (gameState != null) gameState.onHouseBuilt.AddListener(OnHouseBuilt);

            foreach (var n in FindObjectsOfType<ForestNode>())
            {
                _nodes.Add(n);
                n.onDepleted.AddListener(OnNodeDepleted);
            }

            StartCoroutine(AssignTick());
        }

        private void OnHouseBuilt() { _stopped = false; }

        private void OnNodeDepleted(ResourceNode node)
        {
            if (node is ForestNode forestNode) _nodes.Remove(forestNode);
        }

        private IEnumerator AssignTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(assignTickInterval);
                TryAssignTasks();
            }
        }

        private void TryAssignTasks()
        {
            if (_stopped) return;
            if (gameState == null || !gameState.HasAxe) return;

            if (ResourceManager.Instance != null)
            {
                var woodData = ResourceManager.Instance.GetResourceData("Wood");
                if (woodData != null && woodData.count >= woodData.currentMax) return;
            }

            var villagers = FindObjectsOfType<VillagerController>();
            foreach (var v in villagers)
            {
                bool isFree = v.CurrentState == VillagerController.VillagerState.Idle ||
                              v.CurrentState == VillagerController.VillagerState.Walking ||
                              v.CurrentState == VillagerController.VillagerState.Investigating ||
                              v.CurrentState == VillagerController.VillagerState.Gathering;

                if (!isFree) continue;
                if (!v.HasPersonalAxe) continue;
                if (v.IsExhausted)     continue;

                // 1. PRIORITA': Cerca cubi di legno a terra (ResourcePickup)
                ResourcePickup nearestPickup = FindNearestWoodPickup(v.transform.position, v.perceptionRadius * 2f);
                if (nearestPickup != null)
                {
                    // Forza il villager ad andare a prenderlo
                    v.ReceiveResource("Wood", 0); // Reset state to trigger picking logic
                    v.MoveToSocialTarget(nearestPickup.transform.position, 1.2f);
                    Debug.Log($"[ForestManager] {v.name} deviato verso RACCOLTA CUBO.");
                    continue; // Task assegnato
                }

                // 2. SECONDARIO: Cerca alberi intatti
                ForestNode nearestNode = FindNearestIntactNode(v.transform.position);
                if (nearestNode != null)
                {
                    v.AssignResourceTask(nearestNode);
                }
            }
        }

        private ResourcePickup FindNearestWoodPickup(Vector3 from, float radius)
        {
            ResourcePickup best = null;
            float minDist = radius;
            var pickups = FindObjectsOfType<ResourcePickup>();
            foreach (var p in pickups)
            {
                if (p.resourceType != "Wood") continue;
                float d = Vector3.Distance(from, p.transform.position);
                if (d < minDist) { minDist = d; best = p; }
            }
            return best;
        }

        private ForestNode FindNearestIntactNode(Vector3 from)
        {
            ForestNode best = null;
            float minDist = float.MaxValue;
            foreach (var n in _nodes)
            {
                if (n.State != ForestNode.NodeState.Intact) continue;
                float d = Vector3.Distance(from, n.transform.position);
                if (d < minDist) { minDist = d; best = n; }
            }
            return best;
        }
    }
}
