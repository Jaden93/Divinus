using System.Collections;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Ascia lasciata a terra dal giocatore.
    /// Ogni 2s cerca il villager idle più vicino senza ascia assegnata
    /// e lo manda a raccoglierla. Il villager camminaTerso l'ascia e al
    /// contatto chiama Collect() per prendere l'ascia e iniziare a lavorare.
    /// </summary>
    public class AxePickup : MonoBehaviour
    {
        [Header("Ricerca villager")]
        public float searchInterval  = 2f;
        public float searchRadius    = 50f;  // 0 = illimitato

        private GameStateSystem _gameState;
        private bool _claimed = false;

        private void Start()
        {
            _gameState = FindObjectOfType<GameStateSystem>();
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
                // Solo villager idle, non esausti, senza ascia personale
                if (v.HasPersonalAxe) continue;
                if (v.IsExhausted)    continue;
                if (v.CurrentState != VillagerController.VillagerState.Idle &&
                    v.CurrentState != VillagerController.VillagerState.Walking) continue;

                float d = Vector3.Distance(v.transform.position, transform.position);
                if (d < minDist) { minDist = d; best = v; }
            }

            if (best != null)
            {
                _claimed = true;
                best.WalkToAxePickup(this);
                Debug.Log($"[AxePickup] Villager {best.name} si dirige all'ascia.");
            }
        }

        /// <summary>
        /// Chiamato da VillagerController quando arriva all'ascia.
        /// </summary>
        public void Collect(VillagerController villager)
        {
            villager.HasPersonalAxe = true;
            if (_gameState != null) _gameState.HasAxe = true;
            Debug.Log("[AxePickup] Ascia raccolta da " + villager.name);
            Destroy(gameObject);
        }
    }
}
