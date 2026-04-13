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
            StartCoroutine(CheckForNearbyCorpsesRoutine());
        }

        void OnDestroy()
        {
            DivineEventManager.OnDivineEvent -= HandleDivineEvent;
        }

        private IEnumerator CheckForNearbyCorpsesRoutine()
        {
            while (true)
            {
                if (_controller.CurrentState == VillagerController.VillagerState.Idle || _controller.CurrentState == VillagerController.VillagerState.Walking)
                {
                    CheckForNearbyCorpses();
                }
                yield return new WaitForSeconds(3f);
            }
        }

        private void CheckForNearbyCorpses()
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Dead) return;
            if (_controller.CurrentState == VillagerController.VillagerState.CarryingCorpse) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, _controller.perceptionRadius);
            foreach (var hit in hits)
            {
                var other = hit.GetComponent<VillagerController>();
                if (other != null && other.CurrentState == VillagerController.VillagerState.Dead)
                {
                    HandleCorpseDiscovery(other);
                    break;
                }
            }
        }

        private void HandleCorpseDiscovery(VillagerController corpse)
        {
            var p = _controller.personality;
            if (p == null) return;

            switch (p.primaryTrait)
            {
                case PersonalityTrait.Cowardly:
                    StartCoroutine(ReactToScaryEvent(corpse.transform.position));
                    break;

                case PersonalityTrait.Courageous:
                case PersonalityTrait.Altruistic:
                    if (CemeteryController.Instance != null)
                    {
                        StartCoroutine(BurialRoutine(corpse));
                    }
                    else
                    {
                        _eventPos = corpse.transform.position;
                        _lastEventType = DivineEventType.None;
                        StartCoroutine(ReactToEvent());
                    }
                    break;

                case PersonalityTrait.Selfish:
                    if (Random.value > 0.8f && FloatingTextSpawner.Instance != null)
                        FloatingTextSpawner.Instance.Spawn("🙄", transform.position + Vector3.up * 2.5f, Color.gray);
                    break;

                default:
                    _eventPos = corpse.transform.position;
                    _lastEventType = DivineEventType.None;
                    StartCoroutine(ReactToEvent());
                    break;
            }
        }

        private IEnumerator BurialRoutine(VillagerController corpse)
        {
            _controller.PauseWork();
            _controller.SetSocialState(VillagerController.VillagerState.PickingUpCorpse);
            
            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("I will help...", transform.position + Vector3.up * 2.5f, Color.white);

            float timeout = 10f;
            while (Vector3.Distance(transform.position, corpse.transform.position) > 1.5f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.PickingUpCorpse) yield break;
                _controller.MoveToSocialTarget(corpse.transform.position, 1.2f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (timeout <= 0) { _controller.ResumeWork(); yield break; }
            if (_controller.CurrentState != VillagerController.VillagerState.PickingUpCorpse) yield break;

            _controller.SetSocialState(VillagerController.VillagerState.CarryingCorpse);
            corpse.gameObject.SetActive(false); 
            
            Vector3 cemeteryPos = CemeteryController.Instance.GetBurialPosition();
            timeout = 15f;
            while (Vector3.Distance(transform.position, cemeteryPos) > 2.0f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.CarryingCorpse) yield break;
                _controller.MoveToSocialTarget(cemeteryPos, 0.8f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (_controller.CurrentState != VillagerController.VillagerState.CarryingCorpse) yield break;
            _controller.SetSocialState(VillagerController.VillagerState.Burying);
            yield return new WaitForSeconds(3f);
            
            if (_controller.CurrentState == VillagerController.VillagerState.Burying)
            {
                CemeteryController.Instance.BuryCorpse(_controller, corpse);
                _controller.ResumeWork();
            }
        }

        private float _heresyTimer = 0f;
        private float _heresyCheckInterval = 30f;

        void Update()
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Dead) return;

            if (_controller.loyalty < 30f && _controller.CurrentState == VillagerController.VillagerState.Idle)
            {
                _heresyTimer += Time.deltaTime;
                if (_heresyTimer >= _heresyCheckInterval)
                {
                    _heresyTimer = 0f;
                    if (Random.value > 0.7f)
                    {
                        StartCoroutine(StartHeresyGathering());
                    }
                }
            }
        }

        private void Speak(string message, float duration = 3f)
        {
            var hud = GetComponent<VillagerEnergyBar>();
            if (hud != null) hud.ShowSpeech(message, duration);
        }

        private IEnumerator StartHeresyGathering()
        {
            _controller.SetSocialState(VillagerController.VillagerState.Gathering);
            Speak("📣 REBEL! We don't need gods!");

            float duration = Random.Range(15f, 25f);
            while (duration > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Gathering) yield break;
                duration -= 2f;
                InviteNearbyToGathering();
                if (Random.value > 0.7f) Speak("😡", 1.5f);
                yield return new WaitForSeconds(2f);
            }

            if (_controller.CurrentState == VillagerController.VillagerState.Gathering)
                _controller.ResumeWork();
        }

        private void InviteNearbyToGathering()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _controller.perceptionRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var otherReaction = hit.GetComponent<VillagerSocialReaction>();
                if (otherReaction != null)
                {
                    if (Random.value > 0.8f)
                    {
                        otherReaction.StartCoroutine("JoinGathering", transform.position);
                    }
                }
            }
        }

        private IEnumerator JoinGathering(Vector3 spot)
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Gathering || 
                _controller.CurrentState == VillagerController.VillagerState.Investigating) yield break;

            _controller.PauseWork();
            _controller.SetSocialState(VillagerController.VillagerState.Investigating);
            
            Vector3 joinPos = spot + Random.insideUnitSphere * 3f;
            joinPos.y = transform.position.y;
            
            float timeout = 5f;
            while (Vector3.Distance(transform.position, joinPos) > 1.2f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Investigating) yield break;
                _controller.MoveToSocialTarget(joinPos, 1.0f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (_controller.CurrentState != VillagerController.VillagerState.Investigating) yield break;
            _controller.SetSocialState(VillagerController.VillagerState.Gathering);
            Speak("🤔 Tell me more...", 2f);

            yield return new WaitForSeconds(Random.Range(8f, 12f));
            if (_controller.CurrentState == VillagerController.VillagerState.Gathering)
                _controller.ResumeWork();
        }

        public void OnSeeVillager(VillagerController other)
        {
            if (_controller.CurrentState != VillagerController.VillagerState.Idle && 
                _controller.CurrentState != VillagerController.VillagerState.Walking) return;

            bool iAmHeretic = _controller.loyalty <= 20f;
            bool iAmHoly = _controller.loyalty >= 80f;
            bool otherIsHeretic = other.loyalty <= 20f;
            bool otherIsHoly = other.loyalty >= 80f;

            if (iAmHeretic && otherIsHoly)
            {
                if (Random.value > 0.2f) StartCoroutine(ConfrontRoutine(other, true));
            }
            else if (iAmHoly && otherIsHeretic)
            {
                if (Random.value > 0.2f) StartCoroutine(ConfrontRoutine(other, false));
            }
        }

        private IEnumerator ConfrontRoutine(VillagerController target, bool isAggressor)
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Confronting) yield break;
            
            _controller.PauseWork();
            _controller.SetSocialState(VillagerController.VillagerState.Confronting);
            
            Speak(isAggressor ? "💢 HEY YOU! Your god is a lie!" : "🙏 Peace, brother...", 3f);

            float timeout = 5f;
            while (Vector3.Distance(transform.position, target.transform.position) > 1.5f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Confronting) yield break;
                _controller.MoveToSocialTarget(target.transform.position, 1.5f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (timeout <= 0) { _controller.ResumeWork(); yield break; }
            if (_controller.CurrentState != VillagerController.VillagerState.Confronting) yield break;

            _controller.SetSocialState(VillagerController.VillagerState.Gathering);
            Vector3 lookDir = (target.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

            float confrontTimer = 3f;
            while (confrontTimer > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Gathering) yield break;
                confrontTimer -= 1.0f;
                if (Random.value > 0.5f) Speak(isAggressor ? "😡" : "✨", 1f);
                yield return new WaitForSeconds(1.0f);
            }

            if (_controller.CurrentState != VillagerController.VillagerState.Gathering) yield break;

            if (isAggressor && Random.value > 0.3f)
            {
                StartCoroutine(BrawlRoutine(target));
            }
            else if (!isAggressor && Random.value > 0.7f)
            {
                 StartCoroutine(BrawlRoutine(target));
            }
            else
            {
                _controller.ResumeWork();
            }
        }

        private IEnumerator BrawlRoutine(VillagerController target)
        {
            _controller.SetSocialState(VillagerController.VillagerState.Confronting);
            Speak("👊 BRAWL!");

            bool targetIsSainlty = target.loyalty >= 80f;
            float brawlTimer = 15f; 
            
            while (brawlTimer > 0 && target.Health > 0 && _controller.Health > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Confronting) yield break;
                brawlTimer -= 1f;
                if (Random.value > 0.7f) Speak("💢", 0.5f);
                float damage = Random.Range(8f, 15f);
                target.ModifyHealth(-damage);

                if (targetIsSainlty)
                {
                    if (target.Health >= 40f)
                    {
                        target.GetComponent<VillagerSocialReaction>()?.Speak("🙏 HELP ME GOD!", 1.5f);
                    }
                    else
                    {
                        target.GetComponent<VillagerSocialReaction>()?.Speak("😭 NO HELP... I'M LEAVING!", 3f);
                        target.ModifyLoyalty(-(target.loyalty - 50f)); 
                        StartCoroutine(target.GetComponent<VillagerSocialReaction>().ReactToScaryEvent(transform.position));
                        break; 
                    }
                }
                else
                {
                    if (target.Health > 0) _controller.ModifyHealth(-damage * 0.5f);
                }
                yield return new WaitForSeconds(1f);
            }

            if (_controller.CurrentState == VillagerController.VillagerState.Confronting)
                _controller.ResumeWork();
        }

        private void HandleDivineEvent(DivineEvent e)
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Dead) return;
            if (_controller.CurrentState == VillagerController.VillagerState.CarryingCorpse) return;

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
            var p = _controller.personality;
            bool isScary = _lastEventType == DivineEventType.Smite;

            if (isScary)
            {
                switch (p?.primaryTrait)
                {
                    case PersonalityTrait.Cowardly:
                        yield return StartCoroutine(ReactToScaryEvent(_eventPos));
                        break;
                    case PersonalityTrait.Courageous:
                    case PersonalityTrait.Altruistic:
                        yield return StartCoroutine(ReactToCuriousEvent(_eventPos));
                        break;
                    case PersonalityTrait.Devout:
                        _controller.PauseWork();
                        _controller.SetSocialState(VillagerController.VillagerState.Gathering);
                        if (FloatingTextSpawner.Instance != null)
                            FloatingTextSpawner.Instance.Spawn("🙏 Divine Wrath...", transform.position + Vector3.up * 2.5f, Color.yellow);
                        yield return new WaitForSeconds(5f);
                        if (_controller.CurrentState == VillagerController.VillagerState.Gathering)
                            _controller.ResumeWork();
                        break;
                    case PersonalityTrait.Selfish:
                        if (FloatingTextSpawner.Instance != null)
                            FloatingTextSpawner.Instance.Spawn("🙄 Whatever", transform.position + Vector3.up * 2.5f, Color.gray);
                        yield return new WaitForSeconds(1f);
                        break;
                    default:
                        if (Random.value > 0.5f)
                            yield return StartCoroutine(ReactToScaryEvent(_eventPos));
                        else
                            yield return StartCoroutine(ReactToCuriousEvent(_eventPos));
                        break;
                }
            }
            else
            {
                yield return StartCoroutine(ReactToCuriousEvent(_eventPos));
            }
        }

        private IEnumerator ReactToScaryEvent(Vector3 pos)
        {
            _controller.PauseWork();
            _controller.SetSocialState(VillagerController.VillagerState.Messenger);
            _controller.ModifyLoyalty(-15f);

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("😱", transform.position + Vector3.up * 2.5f, Color.red);

            Vector3 fleeDir = (transform.position - pos).normalized;
            if (fleeDir.sqrMagnitude < 0.1f) fleeDir = Random.onUnitSphere;
            Vector3 fleePos = transform.position + fleeDir * 12f;
            
            if (UnityEngine.AI.NavMesh.SamplePosition(fleePos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                fleePos = hit.position;

            float timeout = 4f;
            while (Vector3.Distance(transform.position, fleePos) > 1.5f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Messenger) yield break;
                _controller.MoveToSocialTarget(fleePos, 2.0f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(Random.Range(2f, 4f));
            if (_controller.CurrentState == VillagerController.VillagerState.Messenger)
                _controller.ResumeWork();
        }

        private IEnumerator ReactToCuriousEvent(Vector3 pos)
        {
            _controller.PauseWork();
            _controller.SetSocialState(VillagerController.VillagerState.Investigating);
            _controller.ModifyLoyalty(5f);

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("✨", transform.position + Vector3.up * 2.5f, Color.cyan);

            Vector3 investigatePos = pos + Random.insideUnitSphere * 2.5f;
            investigatePos.y = transform.position.y;
            
            float timeout = 6f;
            while (Vector3.Distance(transform.position, investigatePos) > 1.5f && timeout > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Investigating) yield break;
                _controller.MoveToSocialTarget(investigatePos, 1.2f);
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (_controller.CurrentState != VillagerController.VillagerState.Investigating) yield break;
            _controller.SetSocialState(VillagerController.VillagerState.Gathering);
            Vector3 lookDir = (pos - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookDir);

            float gatherTimer = Random.Range(5f, 9f);
            while (gatherTimer > 0)
            {
                if (_controller.CurrentState != VillagerController.VillagerState.Gathering) yield break;
                if (Random.value > 0.7f && FloatingTextSpawner.Instance != null)
                {
                    string[] emojis = { "🤔", "💬", "🧐", "🙏" };
                    FloatingTextSpawner.Instance.Spawn(emojis[Random.Range(0, emojis.Length)], transform.position + Vector3.up * 2.5f, Color.white);
                }
                AverageLoyaltyWithNearby();
                gatherTimer -= 2f;
                yield return new WaitForSeconds(2f);
            }
            if (_controller.CurrentState == VillagerController.VillagerState.Gathering)
                _controller.ResumeWork();
        }

        private void AverageLoyaltyWithNearby()
        {
            if (_controller.loyalty >= 80f || _controller.loyalty <= 30f) return;
            Collider[] hits = Physics.OverlapSphere(transform.position, 4f);
            float sum = _controller.loyalty; int count = 1;
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var other = hit.GetComponent<VillagerController>();
                if (other != null && (other.CurrentState == VillagerController.VillagerState.Gathering || other.CurrentState == VillagerController.VillagerState.Investigating))
                { sum += other.loyalty; count++; }
            }
            if (count > 1) { float avg = sum / count; float diff = avg - _controller.loyalty; _controller.ModifyLoyalty(diff * 0.1f); }
        }
    }
}
