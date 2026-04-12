using UnityEngine;
using System.Collections.Generic;

namespace DivinePrototype
{
    /// <summary>
    /// Handles the Field of View for a villager, detecting others within a conic area.
    /// </summary>
    public class VillagerVision : MonoBehaviour
    {
        private VillagerController _controller;
        
        [Header("FOV Settings")]
        public float viewRadius = 15f; // Increased radius
        [Range(0, 360)]
        public float viewAngle = 160f; // Wider angle
        public float checkInterval = 0.3f; // Faster check
        
        [Header("Layer Masks")]
        public LayerMask targetMask; // Should include the Villager layer
        public LayerMask obstacleMask; // Should include buildings/walls
        
        private List<VillagerController> _visibleVillagers = new List<VillagerController>();
        private float _timer;

        void Start()
        {
            _controller = GetComponent<VillagerController>();
            // Default layer setup if not assigned
            if (targetMask == 0) targetMask = LayerMask.GetMask("Default"); 
        }

        void Update()
        {
            if (_controller == null || _controller.CurrentState == VillagerController.VillagerState.Dead) return;

            _timer += Time.deltaTime;
            if (_timer >= checkInterval)
            {
                _timer = 0f;
                FindVisibleVillagers();
            }
        }

        void FindVisibleVillagers()
        {
            _visibleVillagers.Clear();
            Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                GameObject target = targetsInViewRadius[i].gameObject;
                if (target == gameObject) continue;

                var other = target.GetComponent<VillagerController>();
                if (other == null || other.CurrentState == VillagerController.VillagerState.Dead) continue;

                Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(transform.position, target.transform.position);
                    if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, dirToTarget, dstToTarget, obstacleMask))
                    {
                        _visibleVillagers.Add(other);
                        NotifySocialReaction(other);
                    }
                }
            }
        }

        private void NotifySocialReaction(VillagerController other)
        {
            // Simple logic: if I see someone with a very different ideology, I might react
            var reaction = GetComponent<VillagerSocialReaction>();
            if (reaction != null)
            {
                reaction.OnSeeVillager(other);
            }
        }

        public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        // Draw FOV in Editor - Always visible for easier debugging of social interactions
        private void OnDrawGizmos()
        {
            if (_controller == null) _controller = GetComponent<VillagerController>();
            if (_controller == null || _controller.CurrentState == VillagerController.VillagerState.Dead) return;

            // FOV Cones
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f); // Faint white for range
            Gizmos.DrawWireSphere(transform.position, viewRadius);

            Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
            Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

            Gizmos.color = _controller.loyalty >= 80f ? Color.yellow : (_controller.loyalty <= 20f ? Color.magenta : Color.white);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

            // Sight Lines to visible targets
            Gizmos.color = Color.red;
            if (Application.isPlaying)
            {
                foreach (var v in _visibleVillagers)
                {
                    if (v != null) Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, v.transform.position + Vector3.up * 1.5f);
                }
            }
            else
            {
                // Simple forward line in editor mode
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Keep the selected view slightly more prominent
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, viewRadius);
        }
    }
}
