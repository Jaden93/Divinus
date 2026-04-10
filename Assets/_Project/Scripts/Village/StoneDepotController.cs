using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Componente sul deposito fisico della pietra posizionato dal giocatore.
    /// Registra il deposito presso StoneDepot (contatore globale) e
    /// aumenta il cap di pietra.
    /// </summary>
    public class StoneDepotController : MonoBehaviour
    {
        private void Start()
        {
            var depot = StoneDepot.Instance;
            if (depot != null)
                depot.SetMaxStone(StoneDepot.MAX_STONE_WITH_DEPOT);

            Debug.Log("[StoneDepotController] Deposito fisico attivo. Cap pietra: " + StoneDepot.MAX_STONE_WITH_DEPOT);
        }

        private void OnDestroy()
        {
            // Se il depot viene distrutto, ricontrolla se ne esistono altri
            var remaining = FindObjectsOfType<StoneDepotController>();
            if (remaining.Length <= 1) // questo oggetto conta ancora durante OnDestroy
            {
                var depot = StoneDepot.Instance;
                if (depot != null)
                    depot.SetMaxStone(StoneDepot.MAX_STONE_WITHOUT_DEPOT);
            }
        }

        /// <summary>Punto di consegna per i villager.</summary>
        public Vector3 GetDeliveryPosition()
        {
            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetDeliveryPosition(), 1.2f);
        }
    }
}
