using UnityEngine;
using System.Collections.Generic;

namespace DivinePrototype
{
    public class CemeteryController : MonoBehaviour
    {
        public static CemeteryController Instance { get; private set; }

        [Header("Settings")]
        public Transform burialPoint;
        public float faithBoostPerBurial = 5f;
        public float loyaltyBoostRadius = 15f;
        public float loyaltyBoostAmount = 10f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public Vector3 GetBurialPosition()
        {
            return burialPoint != null ? burialPoint.position : transform.position;
        }

        public void BuryCorpse(VillagerController undertaker, VillagerController corpse)
        {
            Debug.Log($"[Cemetery] {corpse.name} buried by {undertaker.name}.");
            
            // Visual/Logic cleanup of corpse
            corpse.gameObject.SetActive(false);
            // In a real game, we might spawn a grave marker here.

            // Reward
            var faith = FindObjectOfType<VillageFaithSystem>();
            if (faith != null) faith.AddFaith(faithBoostPerBurial);

            // Morale boost for witnesses
            Collider[] hits = Physics.OverlapSphere(transform.position, loyaltyBoostRadius);
            foreach (var hit in hits)
            {
                var v = hit.GetComponent<VillagerController>();
                if (v != null && v != undertaker && v.CurrentState != VillagerController.VillagerState.Dead)
                {
                    v.ModifyLoyalty(loyaltyBoostAmount);
                }
            }

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("Rest in Peace", transform.position + Vector3.up * 3f, Color.white);
        }
    }
}
