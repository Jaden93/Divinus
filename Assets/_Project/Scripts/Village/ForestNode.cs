using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Rappresenta un albero interagibile. Gestisce lo stato (Intact → Depleted)
    /// e la sostituzione visiva con il moncone quando esaurito.
    /// </summary>
    public class ForestNode : MonoBehaviour
    {
        public enum NodeState { Intact, BeingChopped, Depleted }

        [Header("Setup")]
        public GameObject stumpPrefab;      // TreeStump.prefab
        public int woodAmount = 3;          // legna prodotta da questo albero

        [Header("Events")]
        public UnityEvent<ForestNode> onDepleted;

        public NodeState State { get; private set; } = NodeState.Intact;

        private VillagerController _assignedVillager;

        /// <summary>
        /// Assegna un popolano a questo albero. Ritorna false se già occupato o esaurito.
        /// </summary>
        public bool TryAssign(VillagerController villager)
        {
            if (State != NodeState.Intact) return false;
            _assignedVillager = villager;
            State = NodeState.BeingChopped;
            return true;
        }

        /// <summary>
        /// Chiamato dal popolano quando finisce di tagliare.
        /// Sostituisce la mesh con il moncone e notifica i listener.
        /// </summary>
        public void Deplete()
        {
            if (State == NodeState.Depleted) return;

            State = NodeState.Depleted;
            _assignedVillager = null;

            if (stumpPrefab != null)
            {
                Instantiate(stumpPrefab, transform.position, transform.rotation);
            }

            gameObject.SetActive(false);
            onDepleted?.Invoke(this);
        }

        /// <summary>
        /// Libera l'assegnazione senza depletare (es. popolano interrotto).
        /// </summary>
        public void Release()
        {
            if (State == NodeState.BeingChopped)
                State = NodeState.Intact;
            _assignedVillager = null;
        }
    }
}
