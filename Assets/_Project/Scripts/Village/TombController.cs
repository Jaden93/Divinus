using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Rappresenta una tomba nel cimitero.
    /// Contiene il riferimento al villager sepolto e permette la resurrezione.
    /// </summary>
    public class TombController : MonoBehaviour
    {
        public VillagerController buriedVillager;

        public void Initialize(VillagerController villager)
        {
            buriedVillager = villager;
            // Assicuriamoci che il corpo sia disattivato
            if (buriedVillager != null)
            {
                buriedVillager.gameObject.SetActive(false);
            }
        }

        public void ReviveBuriedVillager()
        {
            if (buriedVillager == null) return;

            // Riattiva il villager nella posizione della tomba
            buriedVillager.gameObject.SetActive(true);
            buriedVillager.transform.position = transform.position + Vector3.up * 0.5f;
            buriedVillager.Revive(0.5f);

            // Effetto visivo
            ReviveVFX.Spawn(transform.position);

            // Rimuovi la tomba
            Destroy(gameObject);
        }
    }
}
