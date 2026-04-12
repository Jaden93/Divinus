using UnityEngine;
using System.Collections;

namespace DivinePrototype
{
    public class VillagerSocialReaction : MonoBehaviour
    {
        private VillagerController _controller;
        private Vector3 _eventPos;
        private DivineEventType _lastEventType;

        void Start()
        {
            _controller = GetComponent<VillagerController>();
            DivineEventManager.OnDivineEvent += HandleDivineEvent;
        }

        void OnDestroy()
        {
            DivineEventManager.OnDivineEvent -= HandleDivineEvent;
        }

        private void HandleDivineEvent(DivineEvent e)
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Dead) return;

            float dist = Vector3.Distance(transform.position, e.Position);
            if (dist <= _controller.perceptionRadius)
            {
                _eventPos = e.Position;
                _lastEventType = e.Type;
                StartCoroutine(ReactToEvent());
            }
        }

        private IEnumerator ReactToEvent()
        {
            _controller.PauseWork();
            bool isScary = _lastEventType == DivineEventType.Smite;
            
            // Loyalty Impact
            float loyaltyImpact = isScary ? -15f : (_lastEventType == DivineEventType.Revive ? 20f : 5f);
            _controller.ModifyLoyalty(loyaltyImpact);

            // Initial emoji reaction
            if (FloatingTextSpawner.Instance != null)
            {
                string emoji = isScary ? "😱" : (_lastEventType == DivineEventType.Revive ? "👼" : "✨");
                Color color = isScary ? Color.red : (_lastEventType == DivineEventType.Revive ? Color.yellow : Color.cyan);
                FloatingTextSpawner.Instance.Spawn(emoji, transform.position + Vector3.up * 2.5f, color);
            }

            if (isScary)
            {
                Debug.Log($"[Social] {name} is TERRORIZED by {_lastEventType}!");
                _controller.SetSocialState(VillagerController.VillagerState.Messenger);
                
                // Flee from event
                Vector3 fleeDir = (transform.position - _eventPos).normalized;
                if (fleeDir.sqrMagnitude < 0.1f) fleeDir = Random.onUnitSphere;
                Vector3 fleePos = transform.position + fleeDir * 12f;
                
                if (UnityEngine.AI.NavMesh.SamplePosition(fleePos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                    fleePos = hit.position;

                float timeout = 4f;
                while (Vector3.Distance(transform.position, fleePos) > 1.5f && timeout > 0)
                {
                    _controller.MoveToSocialTarget(fleePos, 2.0f);
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(Random.Range(2f, 4f));
            }
            else
            {
                Debug.Log($"[Social] {name} is CURIOUS about {_lastEventType}!");
                _controller.SetSocialState(VillagerController.VillagerState.Investigating);
                
                Vector3 investigatePos = _eventPos + Random.insideUnitSphere * 2.5f;
                investigatePos.y = _controller.transform.position.y;
                
                float timeout = 6f;
                while (Vector3.Distance(transform.position, investigatePos) > 1.5f && timeout > 0)
                {
                    _controller.MoveToSocialTarget(investigatePos, 1.2f);
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                // Phase 2: Gathering (Observing/Discussing)
                _controller.SetSocialState(VillagerController.VillagerState.Gathering);
                
                Vector3 lookDir = (_eventPos - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

                // Discuss with emojis and Average Loyalty
                float gatherTimer = Random.Range(5f, 9f);
                while (gatherTimer > 0)
                {
                    if (Random.value > 0.7f && FloatingTextSpawner.Instance != null)
                    {
                        string[] emojis = { "🤔", "💬", "🧐", "🙏" };
                        FloatingTextSpawner.Instance.Spawn(emojis[Random.Range(0, emojis.Length)], transform.position + Vector3.up * 2.5f, Color.white);
                    }

                    // Social Influence: Average loyalty with nearby villagers also gathering
                    AverageLoyaltyWithNearby();

                    gatherTimer -= 2f;
                    yield return new WaitForSeconds(2f);
                }
            }

            _controller.ResumeWork();
        }

        private void AverageLoyaltyWithNearby()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 4f);
            float sum = _controller.loyalty;
            int count = 1;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var other = hit.GetComponent<VillagerController>();
                if (other != null && (other.CurrentState == VillagerController.VillagerState.Gathering || other.CurrentState == VillagerController.VillagerState.Investigating))
                {
                    sum += other.loyalty;
                    count++;
                }
            }

            if (count > 1)
            {
                float avg = sum / count;
                // Move 10% towards average per tick
                float diff = avg - _controller.loyalty;
                _controller.ModifyLoyalty(diff * 0.1f);
            }
        }
    }
}
