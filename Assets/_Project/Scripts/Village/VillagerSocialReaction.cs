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
            Debug.Log($"[Social] {name} reacted to {_lastEventType}!");
            
            // Phase 1: Investigating (Move to spot)
            Vector3 investigatePos = _eventPos + Random.insideUnitSphere * 2f;
            investigatePos.y = _controller.transform.position.y;
            
            yield return new WaitForSeconds(Random.Range(0.2f, 1f));

            float timeout = 5f;
            while(Vector3.Distance(_controller.transform.position, investigatePos) > 1.5f && timeout > 0)
            {
                _controller.transform.position = Vector3.MoveTowards(_controller.transform.position, investigatePos, _controller.moveSpeed * Time.deltaTime);
                Vector3 dir = (investigatePos - _controller.transform.position).normalized;
                dir.y = 0;
                if(dir.sqrMagnitude > 0.01f) 
                {
                    _controller.transform.rotation = Quaternion.Slerp(_controller.transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
                }
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Phase 2: Gathering
            Vector3 lookDir = (_eventPos - _controller.transform.position).normalized;
            lookDir.y = 0;
            if(lookDir.sqrMagnitude > 0.01f) 
            {
                _controller.transform.rotation = Quaternion.LookRotation(lookDir);
            }
            
            yield return new WaitForSeconds(Random.Range(3f, 6f));

            // Phase 3: Messenger
            if (Random.value > 0.6f)
            {
                Debug.Log($"[Social] {name} became a messenger!");
                Vector3 runAway = _controller.transform.position - (_eventPos - _controller.transform.position).normalized * 10f;
                runAway.y = _controller.transform.position.y;
                
                timeout = 4f;
                while(Vector3.Distance(_controller.transform.position, runAway) > 1.5f && timeout > 0)
                {
                    _controller.transform.position = Vector3.MoveTowards(_controller.transform.position, runAway, _controller.moveSpeed * 1.5f * Time.deltaTime);
                    Vector3 dir = (runAway - _controller.transform.position).normalized;
                    dir.y = 0;
                    if(dir.sqrMagnitude > 0.01f) 
                    {
                        _controller.transform.rotation = Quaternion.Slerp(_controller.transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                    }
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }

            _controller.ResumeWork();
        }
    }
}
