using System.Collections;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Piccone lasciato a terra dal giocatore.
    /// Funziona come l'AxePickup ma per il mining.
    /// </summary>
    public class PickaxePickup : MonoBehaviour
    {
        [Header("Ricerca villager")]
        public float searchInterval  = 2f;
        public float searchRadius    = 50f;

        private bool _claimed = false;

        private void Start()
        {
            StartCoroutine(SearchRoutine());
        }

        private IEnumerator SearchRoutine()
        {
            while (!_claimed)
            {
                yield return new WaitForSeconds(searchInterval);
                TryAssignToVillager();
            }
        }

        private void TryAssignToVillager()
        {
            VillagerController best = null;
            float minDist = searchRadius > 0 ? searchRadius : float.MaxValue;

            foreach (var v in FindObjectsOfType<VillagerController>())
            {
                if (v.HasPersonalPickaxe) continue;
                if (v.IsExhausted) continue;
                if (v.CurrentState != VillagerController.VillagerState.Idle &&
                    v.CurrentState != VillagerController.VillagerState.Walking) continue;

                float d = Vector3.Distance(v.transform.position, transform.position);
                if (d < minDist) { minDist = d; best = v; }
            }

            if (best != null)
            {
                _claimed = true;
                best.WalkToPickaxePickup(this);
                Debug.Log($"[PickaxePickup] Villager {best.name} si dirige al piccone.");
            }
        }

        public void Collect(VillagerController villager)
        {
            villager.HasPersonalPickaxe = true;
            Debug.Log("[PickaxePickup] Piccone raccolto da " + villager.name);
            Destroy(gameObject);
        }
    }
}
