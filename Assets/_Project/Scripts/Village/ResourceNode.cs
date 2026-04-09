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

        [Header("Events")]
        public UnityEvent<ResourceNode> onDepleted;

        public NodeState State { get; protected set; } = NodeState.Intact;
        protected VillagerController assignedVillager;

        /// <summary>
        /// Assegna un villager a questa risorsa.
        /// </summary>
        public virtual bool TryAssign(VillagerController villager)
        {
            if (State != NodeState.Intact || amount <= 0) return false;
            assignedVillager = villager;
            State = NodeState.BeingChopped;
            return true;
        }

        /// <summary>
        /// Libera l'assegnazione senza depletare.
        /// </summary>
        public virtual void Release()
        {
            if (State == NodeState.BeingChopped)
                State = NodeState.Intact;
            assignedVillager = null;
        }

        /// <summary>
        /// Preleva una quantità specifica. Se arriva a zero, attiva Deplete().
        /// </summary>
        public virtual int TakeResource(int requestedAmount)
        {
            int taken = Mathf.Min(requestedAmount, amount);
            amount -= taken;

            if (amount <= 0)
            {
                Deplete();
            }
            else
            {
                Release();
            }

            return taken;
        }

        /// <summary>
        /// Gestisce la distruzione o l'animazione finale della risorsa.
        /// </summary>
        public virtual void Deplete()
        {
            if (State == NodeState.Depleted) return;
            State = NodeState.Depleted;
            onDepleted?.Invoke(this);
            OnDepleteVisuals();
        }

        /// <summary>
        /// Da implementare nelle classi derivate per effetti visivi specifici (es. caduta albero).
        /// </summary>
        protected abstract void OnDepleteVisuals();
    }
}
