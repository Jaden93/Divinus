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
            
            // --- SPAWN TOMB ---
            Vector3 tombPos = undertaker.transform.position + undertaker.transform.forward * 0.5f;
            tombPos.y = 0.1f; // Appiattita a terra o leggermente sopra

            GameObject tombGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tombGO.name = $"Tomb_{corpse.name}";
            tombGO.transform.position = tombPos;
            tombGO.transform.localScale = new Vector3(1.2f, 0.2f, 2.0f); // Forma a lastra tombale
            
            var rend = tombGO.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(0.4f, 0.4f, 0.45f); // Grigio pietra

            var tomb = tombGO.AddComponent<TombController>();
            tomb.Initialize(corpse);

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
