using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Componente sul depot fisico posizionato dal giocatore.
    /// Registra il deposit presso WoodDepot (contatore globale) e
    /// aumenta il cap di legna a 30.
    /// </summary>
    public class WoodDepotController : MonoBehaviour
    {
        private void Start()
        {
            var depot = WoodDepot.Instance;
            if (depot != null)
                depot.SetMaxWood(WoodDepot.MAX_WOOD_WITH_DEPOT);

            Debug.Log("[WoodDepotController] Depot fisico attivo. Cap legna: " + WoodDepot.MAX_WOOD_WITH_DEPOT);
        }

        private void OnDestroy()
        {
            // Se il depot viene distrutto, ricontrolla se ne esistono altri
            var remaining = FindObjectsOfType<WoodDepotController>();
            if (remaining.Length <= 1) // questo oggetto conta ancora durante OnDestroy
            {
                var depot = WoodDepot.Instance;
                if (depot != null)
                    depot.SetMaxWood(WoodDepot.MAX_WOOD_WITHOUT_DEPOT);
            }
        }

        /// <summary>Punto di consegna: davanti al depot.</summary>
        public Vector3 GetDeliveryPosition()
        {
            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetDeliveryPosition(), 1f);
        }
    }
}
