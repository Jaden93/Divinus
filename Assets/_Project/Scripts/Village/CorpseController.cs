using UnityEngine;
using System.Collections;

namespace DivinePrototype
{
    public class CorpseController : MonoBehaviour
    {
        public enum DecayStage { Fresh, Decaying, Skeleton }
        
        [Header("Decay Timers")]
        public float freshDuration = 30f;
        public float decayDuration = 30f;
        
        [Header("VFX")]
        public GameObject crowVFXPrefab;
        public GameObject rotVFXPrefab;

        public DecayStage CurrentStage { get; private set; } = DecayStage.Fresh;
        
        private VillagerController _villager;
        private float _timer = 0f;
        private GameObject _activeCrows;
        private GameObject _activeRot;

        void Start()
        {
            _villager = GetComponent<VillagerController>();
            StartCoroutine(DecayRoutine());
        }

        private IEnumerator DecayRoutine()
        {
            // Fresh Stage
            CurrentStage = DecayStage.Fresh;
            yield return new WaitForSeconds(freshDuration);

            // Decaying Stage
            CurrentStage = DecayStage.Decaying;
            Debug.Log($"[Corpse] {name} is now decaying. Crows are coming...");
            
            if (crowVFXPrefab != null)
            {
                _activeCrows = Instantiate(crowVFXPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
            }
            
            if (rotVFXPrefab != null)
            {
                _activeRot = Instantiate(rotVFXPrefab, transform.position, Quaternion.identity, transform);
            }

            float decayTimer = decayDuration;
            while (decayTimer > 0)
            {
                // Negative impact on nearby villagers
                ApplyAuraMoraleDrop();
                decayTimer -= 5f;
                yield return new WaitForSeconds(5f);
            }

            // Skeleton Stage
            CurrentStage = DecayStage.Skeleton;
            Debug.Log($"[Corpse] {name} is now just a skeleton. It can no longer be revived.");
            
            if (_activeCrows != null) Destroy(_activeCrows);
            // In a real game, we might swap the mesh to a skeleton here.
        }

        private void ApplyAuraMoraleDrop()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 10f);
            foreach (var hit in hits)
            {
                var other = hit.GetComponent<VillagerController>();
                if (other != null && other != _villager && other.CurrentState != VillagerController.VillagerState.Dead)
                {
                    other.ModifyLoyalty(-2f); // Corpses are traumatic
                }
            }
        }

        public void CleanUp()
        {
            if (_activeCrows != null) Destroy(_activeCrows);
            if (_activeRot != null) Destroy(_activeRot);
            Destroy(this); // Remove component when buried or revived
        }
    }
}
