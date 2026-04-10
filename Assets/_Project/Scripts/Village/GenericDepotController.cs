using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Componente sull'edificio fisico del deposito generico.
    /// Accetta legna, pietra e qualsiasi risorsa futura.
    /// </summary>
    public class GenericDepotController : MonoBehaviour
    {
        private void Start()
        {
            // Notifica al manager che un deposito è stato costruito
            ResourceManager.Instance?.RefreshCaps();
            Debug.Log("[GenericDepotController] Deposito generico costruito. Capacità villaggio estesa.");
        }

        private void OnDestroy()
        {
            // Notifica al manager che un deposito è stato rimosso
            ResourceManager.Instance?.RefreshCaps();
        }

        /// <summary>
        /// Restituisce il punto dove i villager devono consegnare le risorse.
        /// </summary>
        public Vector3 GetDeliveryPosition()
        {
            return transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 0.1f, 2));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetDeliveryPosition(), 1.5f);
        }
    }
}
