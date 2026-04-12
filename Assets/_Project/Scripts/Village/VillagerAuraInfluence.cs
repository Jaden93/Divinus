using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Passive loyalty contagion based on aura (Angel/Saint vs Heretic/Dark Angel).
    /// </summary>
    public class VillagerAuraInfluence : MonoBehaviour
    {
        private VillagerController _controller;
        public float influenceRadius = 5f;
        public float checkInterval = 2f;
        private float _timer;

        void Start()
        {
            _controller = GetComponent<VillagerController>();
        }

        void Update()
        {
            if (_controller == null || _controller.CurrentState == VillagerController.VillagerState.Dead) return;

            _timer += Time.deltaTime;
            if (_timer >= checkInterval)
            {
                _timer = 0f;
                ProcessInfluence();
            }
        }

        void ProcessInfluence()
        {
            float loyalty = _controller.loyalty;
            float power = 0f;

            // Define power based on loyalty tiers
            if (loyalty >= 99f) power = 2.0f;      // Angel
            else if (loyalty >= 90f) power = 0.8f; // Saint
            else if (loyalty <= 1f) power = -3.0f; // Dark Angel
            else if (loyalty <= 10f) power = -1.2f; // Heretic
            
            if (Mathf.Abs(power) < 0.1f) return;

            // Find nearby villagers
            Collider[] hits = Physics.OverlapSphere(transform.position, influenceRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                
                var other = hit.GetComponent<VillagerController>();
                if (other != null && other.CurrentState != VillagerController.VillagerState.Dead)
                {
                    // --- NEW LOGIC: Only neutrals (30-80) are influenced ---
                    if (other.loyalty > 30f && other.loyalty < 80f)
                    {
                        other.ModifyLoyalty(power);
                    }
                }
            }
        }
    }
}
