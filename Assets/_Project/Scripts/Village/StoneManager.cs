using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Registry degli StoneNode in scena.
    /// Ogni tick cerca villager idle con piccone e assegna automaticamente
    /// il task di mining alla roccia più vicina disponibile.
    /// </summary>
    public class StoneManager : MonoBehaviour
    {
        [Header("Riferimenti")]
        public GameStateSystem gameState;

        [Header("Tick assegnazione")]
        public float assignTickInterval = 2f;

        private readonly List<StoneNode> _nodes = new List<StoneNode>();

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();

            foreach (var n in FindObjectsOfType<StoneNode>())
            {
                _nodes.Add(n);
                n.onDepleted.AddListener(OnNodeDepleted);
            }

            Debug.Log($"[StoneManager] {_nodes.Count} rocce trovate.");
            StartCoroutine(AssignTick());
        }

        private void OnNodeDepleted(ResourceNode node)
        {
            if (node is StoneNode stoneNode)
            {
                _nodes.Remove(stoneNode);
            }
            Debug.Log($"[StoneManager] Roccia esaurita. Rimaste: {_nodes.Count}");
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
            // Rispetta il cap pietra: non assegnare task se già al massimo
            var depot = StoneDepot.Instance;
            if (depot != null && depot.StoneCount >= depot.MaxStone) return;

            var villagers = FindObjectsOfType<VillagerController>();
            foreach (var v in villagers)
            {
                // Verifica se il villager è libero
                bool isFree = v.CurrentState == VillagerController.VillagerState.Idle ||
                              v.CurrentState == VillagerController.VillagerState.Walking;
                
                if (!isFree) continue;
                if (v.IsExhausted) continue;

                // Per ora controlliamo se ha il piccone tramite un tag o un flag futuro
                // Se non vogliamo toccare VillagerController, potremmo usare:
                // if (!v.GetComponent<VillagerMiningAddon>().HasPickaxe) continue;
                
                // NOTA: Per ora lo commento per non rompere il build se HasPersonalPickaxe non esiste ancora
                // if (!v.HasPersonalPickaxe) continue;

                StoneNode nearest = FindNearestIntactNode(v.transform.position);
                if (nearest != null)
                {
                    // Assegna il task. Se VillagerController non supporta StoneNode,
                    // dovremo estenderlo.
                    v.AssignResourceTask(nearest);
                    Debug.Log($"[StoneManager] Mining assegnato a {v.name} → {nearest.name}");
                }
            }
        }

        private StoneNode FindNearestIntactNode(Vector3 from)
        {
            StoneNode best = null;
            float minDist = float.MaxValue;

            foreach (var n in _nodes)
            {
                if (n.State != StoneNode.NodeState.Intact) continue;
                float d = Vector3.Distance(from, n.transform.position);
                if (d < minDist) { minDist = d; best = n; }
            }

            return best;
        }
    }
}
