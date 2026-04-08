using UnityEngine;
using UnityEngine.AI;

namespace DivinePrototype
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class VillagerController : MonoBehaviour
    {
        public enum VillagerState
        {
            Idle, Walking, ChoppingWood, CarryingWood,
            GoingToSleep, Sleeping,
            PickingUpAxe, Resting,
            GoingToBench, Sitting,
            Dead
        }

        [Header("Movement")]
        public float moveSpeed            = 2f;
        public float waypointStopDistance = 0.3f;
        public float wanderRadius         = 10f;  // raggio random wander

        [Header("Idle")]
        public float idleMinDuration = 2f;
        public float idleMaxDuration = 4f;

        [Header("Work")]
        public float chopDuration     = 3f;
        public float taskStopDistance = 1.5f;

        [Header("Energia")]
        public float maxEnergy                   = 100f;
        public float energyDrainPerSecond        = 8f;
        public float exhaustionThreshold         = 70f;
        public float idleSleepThreshold          = 90f;   // va a riposare se idle con energia < questa soglia
        public float restDuration                = 8f;    // riposo in place se non c'e' casa
        public float restRestoreAmount           = 60f;   // energia totale recuperata dal riposo in place
        public float sleepEnergyRestorePerSecond = 15f;   // energia per secondo durante il sonno in casa

        // ── Stato pubblico ─────────────────────────────────────────────
        public VillagerState CurrentState { get; private set; } = VillagerState.Idle;
        public float Energy               { get; private set; }
        public bool  IsExhausted          => Energy <= exhaustionThreshold;
        public bool  HasPersonalAxe       { get; set; } = false;

        // ── Navigazione ────────────────────────────────────────────────
        private NavMeshAgent _agent;

        // ── Animator ───────────────────────────────────────────────────
        private Animator _animator;
        private static readonly int ParamWalking   = Animator.StringToHash("isWalking");
        private static readonly int ParamChopping  = Animator.StringToHash("isChopping");
        private static readonly int ParamStartWalk = Animator.StringToHash("startWalking");
        private static readonly int ParamDying     = Animator.StringToHash("dying");

        // ── Stato interno walking ──────────────────────────────────────
        private Vector3 _walkTarget  = Vector3.zero;
        private float   _idleTimer   = 0f;
        private float   _idleDuration = 0f;

        // ── Stato interno task ─────────────────────────────────────────
        private ForestNode         _targetNode;
        private WoodDepotController _targetDepotCtrl;   // depot fisico (può essere null)
        private bool  _arrivedAtNode = false;
        private float _chopTimer     = 0f;

        // ── Stato interno sleep ────────────────────────────────────────
        private Vector3          _sleepTarget;
        private float            _sleepDuration;
        private float            _sleepTimer;
        private System.Action    _onWakeUp;
        private HouseController  _targetHouse;

        // ── Stato interno riposo in place ──────────────────────────────
        private float _restTimer = 0f;

        // ── Stato interno panca ───────────────────────────────────────
        private BenchController _targetBench;
        private float           _sitTimer;

        // ── Stato interno raccolta ascia ───────────────────────────────
        private AxePickup _axePickupTarget;

        // ──────────────────────────────────────────────────────────────

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            
            // Auto-configure components if they were just added or are default
            if (_agent != null)
            {
                _agent.speed                 = moveSpeed;
                _agent.stoppingDistance      = waypointStopDistance;
                _agent.angularSpeed          = 360f;
                _agent.acceleration          = 8f;
                _agent.radius                = 0.35f;
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                _agent.avoidancePriority     = Random.Range(30, 70);
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null && col.height < 0.1f) // default or unconfigured
            {
                col.center = new Vector3(0, 1.07f, 0);
                col.radius = 0.3f;
                col.height = 2.15f;
            }

            Energy = maxEnergy;
            GoIdleDirect();
        }

        private void Update()
        {
            if (CurrentState == VillagerState.Dead) return;

            switch (CurrentState)
            {
                case VillagerState.Idle:
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= _idleDuration) GoWalking();
                    break;

                case VillagerState.Walking:
                    UpdateWalking();
                    break;

                case VillagerState.ChoppingWood:
                    UpdateChoppingWood();
                    break;

                case VillagerState.CarryingWood:
                    UpdateCarryingWood();
                    break;

                case VillagerState.GoingToSleep:
                    UpdateGoingToSleep();
                    break;

                case VillagerState.Sleeping:
                    UpdateSleeping();
                    break;

                case VillagerState.PickingUpAxe:
                    UpdatePickingUpAxe();
                    break;

                case VillagerState.Resting:
                    UpdateResting();
                    break;

                case VillagerState.GoingToBench:
                    UpdateGoingToBench();
                    break;

                case VillagerState.Sitting:
                    UpdateSitting();
                    break;
            }
        }

        // ── Idle / Walking ──────────────────────────────────────────────

        // Passa a Idle senza controllare energia (usato al risveglio/panca)
        private void GoIdleDirect()
        {
            StopAgent();
            CurrentState  = VillagerState.Idle;
            _idleDuration = Random.Range(idleMinDuration, idleMaxDuration);
            _idleTimer    = 0f;
            SetAnim(false, false);
        }

        // Passa a Idle controllando energia: se esaurito va a dormire
        private void GoIdle()
        {
            if (Energy < idleSleepThreshold)
            {
                StartAutoSleep();
                return;
            }
            GoIdleDirect();
        }

        private void GoWalking()
        {
            if (!TryGetRandomNavMeshPoint(out Vector3 dest))
            {
                GoIdleDirect();
                return;
            }
            _walkTarget  = dest;
            CurrentState = VillagerState.Walking;

            if (_animator != null)
            {
                _animator.SetTrigger(ParamStartWalk);
            }

            SetAnim(true, false);
            MoveTo(Flat(dest));
        }

        private void UpdateWalking()
        {
            bool arrived;
            if (_agent != null && _agent.isOnNavMesh)
                arrived = !_agent.pathPending && _agent.remainingDistance <= waypointStopDistance;
            else
                arrived = Vector3.Distance(Flat(transform.position), Flat(_walkTarget)) <= waypointStopDistance;

            if (arrived) GoIdle();
        }

        private bool TryGetRandomNavMeshPoint(out Vector3 result)
        {
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir.y = 0f;
            randomDir  += transform.position;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = transform.position;
            return false;
        }

        // ── Task: Taglia legna ─────────────────────────────────────────

        public void AssignWoodTask(ForestNode node)
        {
            if (node == null) return;
            if (!node.TryAssign(this)) return;

            _targetNode     = node;
            _targetDepotCtrl = FindNearestDepotController();
            _arrivedAtNode  = false;
            _chopTimer      = 0f;
            CurrentState    = VillagerState.ChoppingWood;
        }

        private void UpdateChoppingWood()
        {
            if (_targetNode == null) { GoIdle(); return; }

            // Smetti se il depot è pieno
            var depot = WoodDepot.Instance;
            if (depot != null && depot.WoodCount >= depot.MaxWood)
            {
                _targetNode.Release();
                _targetNode      = null;
                _targetDepotCtrl = null;
                GoIdleDirect();
                return;
            }

            Vector3 nodePos = Flat(_targetNode.transform.position);

            if (!_arrivedAtNode)
            {
                MoveTo(nodePos);
                SetAnim(true, false);

                if (AgentReachedTarget(nodePos, taskStopDistance))
                {
                    _arrivedAtNode = true;
                    StopAgent();
                }
            }
            else
            {
                SetAnim(false, true);
                _chopTimer += Time.deltaTime;

                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);

                if (_chopTimer >= chopDuration)
                {
                    _targetNode.Deplete();
                    GoCarryingWood();
                    return;
                }

                if (IsExhausted)
                {
                    if (_targetNode != null) _targetNode.Release();
                    _targetNode  = null;
                    _targetDepotCtrl = null;
                    StartAutoSleep();
                }
            }
        }

        private void GoCarryingWood()
        {
            // Aggiorna il depot target più vicino al momento della consegna
            _targetDepotCtrl = FindNearestDepotController();
            CurrentState = VillagerState.CarryingWood;
            SetAnim(true, false);
        }

        private void UpdateCarryingWood()
        {
            int woodAmount = _targetNode != null ? _targetNode.woodAmount : 1;

            if (_targetDepotCtrl == null)
            {
                // Nessun depot fisico: consegna istantanea (cap 9)
                WoodDepot.Instance?.DepositWood(woodAmount);
                _targetNode      = null;
                _targetDepotCtrl = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
                return;
            }

            Vector3 depotPos = Flat(_targetDepotCtrl.GetDeliveryPosition());
            MoveTo(depotPos);

            if (AgentReachedTarget(depotPos, taskStopDistance))
            {
                WoodDepot.Instance?.DepositWood(woodAmount);
                _targetNode      = null;
                _targetDepotCtrl = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
            }
        }

        private WoodDepotController FindNearestDepotController()
        {
            WoodDepotController best = null;
            float minDist = float.MaxValue;
            foreach (var d in FindObjectsOfType<WoodDepotController>())
            {
                float dist = Vector3.Distance(transform.position, d.transform.position);
                if (dist < minDist) { minDist = dist; best = d; }
            }
            return best;
        }

        // ── Energia / Sonno automatico ─────────────────────────────────

        private void StartAutoSleep()
        {
            // Priorità: panca libera > casa libera più vicina > riposo in place
            var bench = FindFreeBench();
            if (bench != null) { Debug.Log("[VillagerController] Vado alla panca"); GoToBench(bench); return; }

            var house = FindFreeHouse();
            if (house != null) { Debug.Log($"[VillagerController] Vado a dormire in casa: {house.name}"); GoToHouseAndSleep(house); return; }

            Debug.Log("[VillagerController] Nessuna casa/panca libera, riposo in place");
            GoResting();
        }

        private HouseController FindFreeHouse()
        {
            HouseController best = null;
            float minDist = float.MaxValue;
            foreach (var h in FindObjectsOfType<HouseController>())
            {
                if (h.IsFull) continue;
                float d = Vector3.Distance(transform.position, h.transform.position);
                if (d < minDist) { minDist = d; best = h; }
            }
            return best;
        }

        private BenchController FindFreeBench()
        {
            BenchController best = null;
            float minDist = float.MaxValue;
            foreach (var b in FindObjectsOfType<BenchController>())
            {
                if (b.IsOccupied) continue;
                float d = Vector3.Distance(transform.position, b.transform.position);
                if (d < minDist) { minDist = d; best = b; }
            }
            return best;
        }

        // ── Panca ─────────────────────────────────────────────────────

        public void GoToBench(BenchController bench)
        {
            if (bench == null || !bench.TryOccupy(this)) return;
            if (_targetNode != null) _targetNode.Release();
            _targetNode  = null;
            _targetDepotCtrl = null;
            _targetBench = bench;
            _sitTimer    = 0f;
            CurrentState = VillagerState.GoingToBench;
            SetAnim(true, false);
        }

        private void UpdateGoingToBench()
        {
            if (_targetBench == null) { GoIdleDirect(); return; }

            Vector3 target = Flat(_targetBench.GetSitPosition());
            MoveTo(target);

            if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance)
            {
                StopAgent();
                CurrentState = VillagerState.Sitting;
                _sitTimer    = 0f;
                SetAnim(false, false);
            }
        }

        private void UpdateSitting()
        {
            _sitTimer += Time.deltaTime;
            Energy = Mathf.Min(maxEnergy, Energy + _targetBench.energyRecoveryPerSecond * Time.deltaTime);

            if (_sitTimer >= _targetBench.sitDuration || Energy >= maxEnergy)
            {
                _targetBench.Vacate();
                _targetBench = null;
                Debug.Log($"[VillagerController] Si alza dalla panca. Energia: {Energy:0}");
                GoIdleDirect();
            }
        }

        private void GoResting()
        {
            StopAgent();
            CurrentState = VillagerState.Resting;
            _restTimer   = 0f;
            SetAnim(false, false);
        }

        private void UpdateResting()
        {
            _restTimer += Time.deltaTime;
            Energy = Mathf.Min(maxEnergy, Energy + (restRestoreAmount / restDuration) * Time.deltaTime);

            if (_restTimer >= restDuration || Energy >= maxEnergy)
            {
                Debug.Log($"[VillagerController] Riposo terminato. Energia: {Energy:0}");
                GoIdleDirect();
            }
        }

        // ── Sleep in casa ──────────────────────────────────────────────

        private void GoToHouseAndSleep(HouseController house)
        {
            if (!house.TryOccupy()) { GoResting(); return; }

            if (_targetNode != null) _targetNode.Release();
            _targetNode      = null;
            _targetDepotCtrl = null;

            _targetHouse   = house;
            Vector3 rawSleepPos = house.GetSleepPosition();
            // Snap al punto NavMesh più vicino per garantire raggiungibilità
            if (NavMesh.SamplePosition(rawSleepPos, out NavMeshHit sleepHit, 4f, NavMesh.AllAreas))
                _sleepTarget = sleepHit.position;
            else
                _sleepTarget = rawSleepPos;
            _sleepDuration = house.sleepDuration;
            _sleepTimer    = 0f;
            _onWakeUp      = null;
            CurrentState   = VillagerState.GoingToSleep;
            SetAnim(true, false);

            // Apri la porta subito così il villager vede l'apertura
            _targetHouse?.OpenDoor();
            Debug.Log($"[VillagerController] GoToHouseAndSleep → sleepTarget={_sleepTarget} casa={house.name}");
        }

        private void UpdateGoingToSleep()
        {
            Vector3 target = Flat(_sleepTarget);
            MoveTo(target);

            bool agentStopped = _agent != null && _agent.isOnNavMesh
                && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
            bool closeEnough  = Vector3.Distance(Flat(transform.position), target) <= 1.5f;

            if (agentStopped || closeEnough)
            {
                StopAgent();
                CurrentState = VillagerState.Sleeping;
                _sleepTimer  = 0f;
                SetAnim(false, false);

                // Nascondi il villager (simula ingresso in casa)
                SetVisibility(false);
                _targetHouse?.CloseDoor();
            }
        }

        private void UpdateSleeping()
        {
            Energy = Mathf.Min(maxEnergy, Energy + sleepEnergyRestorePerSecond * Time.deltaTime);
            _sleepTimer += Time.deltaTime;

            if (_sleepTimer >= _sleepDuration || Energy >= maxEnergy)
            {
                Debug.Log($"[VillagerController] Sveglio. Energia: {Energy:0}");

                // Porta si apre, villager ricompare
                var house = _targetHouse;
                house?.OpenDoor();
                SetVisibility(true);

                house?.Vacate();
                _targetHouse = null;
                _sleepTarget = Vector3.zero;
                _onWakeUp?.Invoke();
                _onWakeUp = null;
                GoIdleDirect();

                // Chiudi la porta dopo un breve delay
                if (house != null) StartCoroutine(CloseDoorDelayed(house));
            }
        }

        private System.Collections.IEnumerator CloseDoorDelayed(HouseController house)
        {
            yield return new WaitForSeconds(1.5f);
            house.CloseDoor();
        }

        private void SetVisibility(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = visible;
        }

        // ── Raccolta ascia a terra ─────────────────────────────────────

        public void WalkToAxePickup(AxePickup pickup)
        {
            if (pickup == null) return;
            _axePickupTarget = pickup;
            CurrentState     = VillagerState.PickingUpAxe;
            SetAnim(true, false);
        }

        private void UpdatePickingUpAxe()
        {
            if (_axePickupTarget == null) { GoIdle(); return; }

            Vector3 target = Flat(_axePickupTarget.transform.position);
            MoveTo(target);

            if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance)
            {
                _axePickupTarget.Collect(this);
                _axePickupTarget = null;
                GoIdle();
            }
        }

        // ── Animator ──────────────────────────────────────────────────

        private void SetAnim(bool walking, bool chopping)
        {
            if (_animator == null) return;
            _animator.SetBool(ParamWalking,  walking);
            _animator.SetBool(ParamChopping, chopping);
        }

        // ── Utilita' ───────────────────────────────────────────────────

        private void MoveTo(Vector3 targetFlat)
        {
            Vector3 dest = new Vector3(targetFlat.x, 0f, targetFlat.z);
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                // Evita di resettare pathPending ogni frame: ricalcola solo se destinazione cambiata
                if (!_agent.hasPath || _agent.pathPending ||
                    Vector3.Distance(_agent.destination, dest) > 0.05f)
                {
                    _agent.SetDestination(dest);
                }
                return;
            }
            // Fallback senza NavMesh
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(dest.x, transform.position.y, dest.z),
                moveSpeed * Time.deltaTime);
            Vector3 dir = dest - Flat(transform.position);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        private void StopAgent()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private bool AgentReachedTarget(Vector3 target, float stopDist)
        {
            return Vector3.Distance(Flat(transform.position), Flat(target)) <= stopDist;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // ── API pubblica ──────────────────────────────────────────────

        public void Die()
        {
            if (CurrentState == VillagerState.Dead) return;

            // Release resources
            if (_targetNode != null) _targetNode.Release();
            if (_targetBench != null) _targetBench.Vacate();
            if (_targetHouse != null) _targetHouse.Vacate();

            _targetNode = null;
            _targetBench = null;
            _targetHouse = null;

            StopAgent();
            CurrentState = VillagerState.Dead;

            if (_animator != null)
            {
                _animator.SetTrigger(ParamDying);
            }

            Debug.Log($"[VillagerController] {name} is DEAD.");
        }

        public void Revive(float energyPercent)
        {
            if (CurrentState != VillagerState.Dead) return;

            Energy = maxEnergy * energyPercent;
            SetVisibility(true); // Assicura che sia visibile se era nascosto (es. in casa)
            GoIdleDirect();
            Debug.Log($"[VillagerController] {name} REVIVED.");
        }

        public void ForceIdle()
        {
            if (_targetNode  != null) _targetNode.Release();
            if (_targetBench != null) _targetBench.Vacate();
            _targetNode      = null;
            _targetDepotCtrl = null;
            _targetBench     = null;
            GoIdleDirect();
        }

        public void SetEnergy(float value)
        {
            Energy = Mathf.Clamp(value, 0f, maxEnergy);
        }

        public string GetStateLabel()
        {
            return CurrentState switch
            {
                VillagerState.Idle         => "Idle",
                VillagerState.Walking      => "Walking",
                VillagerState.ChoppingWood => "Chopping",
                VillagerState.CarryingWood => "Carrying Wood",
                VillagerState.GoingToSleep => "-> Sleep",
                VillagerState.Sleeping     => "Sleeping",
                VillagerState.PickingUpAxe => "-> Axe",
                VillagerState.Resting      => "Resting",
                VillagerState.GoingToBench => "-> Bench",
                VillagerState.Sitting      => "Sitting",
                _                          => ""
            };
        }
    }
}
