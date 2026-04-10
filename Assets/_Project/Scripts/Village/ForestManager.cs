using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Registry dei ForestNode in scena.
    /// Ogni tick cerca villager idle con ascia e assegna automaticamente
    /// il task di taglio legna al nodo più vicino disponibile.
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

            if (gameState != null)
                gameState.onHouseBuilt.AddListener(OnHouseBuilt);

            foreach (var n in FindObjectsOfType<ForestNode>())
            {
                _nodes.Add(n);
                n.onDepleted.AddListener(OnNodeDepleted);
            }

            Debug.Log($"[ForestManager] {_nodes.Count} alberi | ResourceManager={ResourceManager.Instance != null}");
            StartCoroutine(AssignTick());
        }

        private void OnHouseBuilt()
        {
            // Dopo la costruzione della casa il villager può riprendere a lavorare
            _stopped = false;
            Debug.Log("[ForestManager] Casa costruita. Assegnazione task ripresa.");
        }

        private void OnNodeDepleted(ResourceNode node)
        {
            if (node is ForestNode forestNode)
            {
                _nodes.Remove(forestNode);
            }
            Debug.Log($"[ForestManager] Risorsa esaurita. Rimasti: {_nodes.Count}");
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

            // Rispetta il cap legna: non assegnare task se già al massimo
            if (ResourceManager.Instance != null)
            {
                var woodData = ResourceManager.Instance.wood;
                if (woodData.count >= woodData.currentMax) return;
            }

            var villagers = FindObjectsOfType<VillagerController>();
            foreach (var v in villagers)
            {
                bool isFree = v.CurrentState == VillagerController.VillagerState.Idle ||
                              v.CurrentState == VillagerController.VillagerState.Walking;
                if (!isFree) continue;
                if (!v.HasPersonalAxe) continue;
                if (v.IsExhausted)     continue;

                ForestNode nearest = FindNearestIntactNode(v.transform.position);
                if (nearest != null)
                {
                    v.AssignResourceTask(nearest);
                    Debug.Log($"[ForestManager] Task assegnato a {v.name} → {nearest.name}");
                }
            }
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
