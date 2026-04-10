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
            MiningStone, CarryingStone,
            GoingToSleep, Sleeping,
            PickingUpAxe, PickingUpPickaxe,
            Resting, GoingToBench, Sitting,
            Dead
        }

        [Header("Movement")]
        public float moveSpeed            = 1.5f; 
        public float waypointStopDistance = 0.3f;
        public float wanderRadius         = 10f;  

        [Header("Idle")]
        public float idleMinDuration = 2f;
        public float idleMaxDuration = 4f;

        [Header("Work")]
        public float chopDuration     = 3f;
        public float taskStopDistance = 1.5f;

        [Header("Energia")]
        public float maxEnergy                   = 100f;
        public float energyDrainPerSecond        = 8f;
        public float exhaustionThreshold         = 20f;
        public float idleSleepThreshold          = 40f;   
        public float restDuration                = 8f;    
        public float restRestoreAmount           = 60f;   
        public float sleepEnergyRestorePerSecond = 15f;   

        [Header("Social")]
        public float loyalty = 50f; // 0-100
        public float perceptionRadius = 20f;

        // ── Stato pubblico ─────────────────────────────────────────────
        public VillagerState CurrentState { get; private set; } = VillagerState.Idle;
        public float Energy               { get; private set; }
        public bool  IsExhausted          => Energy <= exhaustionThreshold;
        public bool  HasPersonalAxe       { get; set; } = false;
        public bool  HasPersonalPickaxe   { get; set; } = false;

        // ── Navigazione ────────────────────────────────────────────────
        private NavMeshAgent _agent;

        // ── Animator ───────────────────────────────────────────────────
        private Animator _animator;
        private static readonly int ParamWalking   = Animator.StringToHash("isWalking");
        private static readonly int ParamChopping  = Animator.StringToHash("isChopping");
        private static readonly int ParamMining    = Animator.StringToHash("isMining");
        private static readonly int ParamDying     = Animator.StringToHash("dying");
        private static readonly int ParamStartWalk = Animator.StringToHash("startWalk");

        // ── Stato interno walking ──────────────────────────────────────
        private Vector3 _walkTarget  = Vector3.zero;
        private float   _idleTimer   = 0f;
        private float   _idleDuration = 0f;

        // ── Stato interno task ─────────────────────────────────────────
        private ResourceNode           _targetResource;
        private GenericDepotController _targetDepot;      // depot fisico generico
        private bool  _arrivedAtNode = false;
        private float _chopTimer     = 0f;
        private int   _carriedWoodAmount = 0;
        private int   _carriedStoneAmount = 0;

        // ── Stato interno sleep ────────────────────────────────────────
        private Vector3          _sleepTarget;
        private Vector3          _doorThreshold;
        private bool             _isEnteringHouse;
        private float            _sleepDuration;
        private float            _sleepTimer;
        private System.Action    _onWakeUp;
        private HouseController  _targetHouse;

        // ── Stato interno riposo in place ──────────────────────────────
        private float _restTimer = 0f;

        // ── Stato interno panca ───────────────────────────────────────
        private BenchController _targetBench;
        private float           _sitTimer;

        // ── Stato interno raccolta strumenti ───────────────────────────
        private AxePickup     _axePickupTarget;
        private PickaxePickup _pickaxePickupTarget;

        // ──────────────────────────────────────────────────────────────

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; 
            rb.useGravity = false;

            if (_agent != null)
            {
                _agent.speed                 = moveSpeed;
                _agent.stoppingDistance      = waypointStopDistance;
                _agent.angularSpeed          = 360f;
                _agent.acceleration          = 10f;
                _agent.radius                = 0.3f;
                _agent.height                = 2.0f;
                _agent.baseOffset            = 0f; 
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                _agent.avoidancePriority     = Random.Range(30, 70);
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0, 1.0f, 0);
                col.radius = 0.3f;
                col.height = 2.0f;
                col.isTrigger = false; 
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

                case VillagerState.MiningStone:
                    UpdateMiningStone();
                    break;

                case VillagerState.CarryingStone:
                    UpdateCarryingStone();
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

                case VillagerState.PickingUpPickaxe:
                    UpdatePickingUpPickaxe();
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

        private void GoIdleDirect()
        {
            StopAgent();
            CurrentState  = VillagerState.Idle;
            _idleDuration = Random.Range(idleMinDuration, idleMaxDuration);
            _idleTimer    = 0f;
            SetAnim(false, false);
        }

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

        // ── Task: Raccolta Risorse ─────────────────────────────────────

        public void AssignResourceTask(ResourceNode node)
        {
            if (node == null) return;
            if (!node.TryAssign(this)) return;

            _targetResource = node;
            _arrivedAtNode  = false;
            _chopTimer      = 0f;
            _targetDepot    = FindNearestDepot();

            if (node.resourceName == "Wood")
            {
                CurrentState = VillagerState.ChoppingWood;
            }
            else if (node.resourceName == "Stone")
            {
                CurrentState = VillagerState.MiningStone;
            }
        }

        private void UpdateChoppingWood()
        {
            if (_targetResource == null) { GoIdle(); return; }

            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                var woodData = rm.GetResourceData("Wood");
                if (woodData != null && woodData.count >= woodData.currentMax)
                {
                    _targetResource.Release();
                    _targetResource = null;
                    _targetDepot    = null;
                    GoIdleDirect();
                    return;
                }
            }

            Vector3 nodePos = Flat(_targetResource.transform.position);

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
                    int spaceLeft = 1;
                    if (rm != null) 
                    {
                        var d = rm.GetResourceData("Wood");
                        if (d != null) spaceLeft = d.currentMax - d.count;
                    }
                    _carriedWoodAmount = _targetResource.TakeResource(spaceLeft);
                    GoCarryingWood();
                    return;
                }

                if (IsExhausted)
                {
                    if (_targetResource != null) _targetResource.Release();
                    _targetResource = null;
                    _targetDepot    = null;
                    StartAutoSleep();
                }
            }
        }

        private void UpdateMiningStone()
        {
            if (_targetResource == null) { GoIdle(); return; }

            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                var stoneData = rm.GetResourceData("Stone");
                if (stoneData != null && stoneData.count >= stoneData.currentMax)
                {
                    _targetResource.Release();
                    _targetResource = null;
                    _targetDepot    = null;
                    GoIdleDirect();
                    return;
                }
            }

            Vector3 nodePos = Flat(_targetResource.transform.position);

            if (!_arrivedAtNode)
            {
                MoveTo(nodePos);
                SetAnim(true, false, false);

                if (AgentReachedTarget(nodePos, taskStopDistance))
                {
                    _arrivedAtNode = true;
                    StopAgent();
                }
            }
            else
            {
                SetAnim(false, false, true);
                _chopTimer += Time.deltaTime;
                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);

                if (_chopTimer >= chopDuration)
                {
                    int spaceLeft = 1;
                    if (rm != null) 
                    {
                        var d = rm.GetResourceData("Stone");
                        if (d != null) spaceLeft = d.currentMax - d.count;
                    }
                    _carriedStoneAmount = _targetResource.TakeResource(spaceLeft);
                    GoCarryingStone();
                    return;
                }

                if (IsExhausted)
                {
                    if (_targetResource != null) _targetResource.Release();
                    _targetResource = null;
                    _targetDepot    = null;
                    StartAutoSleep();
                }
            }
        }

        private void GoCarryingWood()
        {
            _targetDepot = FindNearestDepot();
            CurrentState = VillagerState.CarryingWood;
            SetAnim(true, false);
        }

        private void GoCarryingStone()
        {
            _targetDepot = FindNearestDepot();
            CurrentState = VillagerState.CarryingStone;
            SetAnim(true, false, false);
        }

        private void UpdateCarryingWood()
        {
            if (_targetDepot == null)
            {
                ResourceManager.Instance?.AddResource("Wood", _carriedWoodAmount);
                _carriedWoodAmount = 0;
                _targetResource = null;
                _targetDepot    = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
                return;
            }

            Vector3 depotPos = Flat(_targetDepot.GetDeliveryPosition());
            MoveTo(depotPos);

            if (AgentReachedTarget(depotPos, taskStopDistance))
            {
                ResourceManager.Instance?.AddResource("Wood", _carriedWoodAmount);
                _carriedWoodAmount = 0;
                _targetResource = null;
                _targetDepot    = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
            }
        }

        private void UpdateCarryingStone()
        {
            if (_targetDepot == null)
            {
                ResourceManager.Instance?.AddResource("Stone", _carriedStoneAmount);
                _carriedStoneAmount = 0;
                _targetResource = null;
                _targetDepot    = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
                return;
            }

            Vector3 depotPos = Flat(_targetDepot.GetDeliveryPosition());
            MoveTo(depotPos);

            if (AgentReachedTarget(depotPos, taskStopDistance))
            {
                ResourceManager.Instance?.AddResource("Stone", _carriedStoneAmount);
                _carriedStoneAmount = 0;
                _targetResource = null;
                _targetDepot    = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
            }
        }

        private GenericDepotController FindNearestDepot()
        {
            GenericDepotController best = null;
            float minDist = float.MaxValue;
            foreach (var d in FindObjectsOfType<GenericDepotController>())
            {
                float dist = Vector3.Distance(transform.position, d.transform.position);
                if (dist < minDist) { minDist = dist; best = d; }
            }
            return best;
        }

        // ── Energia / Sonno automatico ─────────────────────────────────

        private void StartAutoSleep()
        {
            var bench = FindFreeBench();
            if (bench != null) { GoToBench(bench); return; }

            var house = FindFreeHouse();
            if (house != null) { GoToHouseAndSleep(house); return; }

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
            if (_targetResource != null) _targetResource.Release();
            _targetResource = null;
            _targetDepot    = null;
            _targetBench    = bench;
            _sitTimer       = 0f;
            CurrentState    = VillagerState.GoingToBench;
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
                GoIdleDirect();
            }
        }

        // ── Sleep in casa ──────────────────────────────────────────────

        private void GoToHouseAndSleep(HouseController house)
        {
            if (!house.TryOccupy()) { GoResting(); return; }
            SetAnim(false, false);
            if (_targetResource != null) _targetResource.Release();
            _targetResource = null;
            _targetDepot    = null;
            _targetHouse    = house;
            _doorThreshold  = house.GetDoorThreshold();
            _sleepTarget    = house.GetSleepPosition();
            _isEnteringHouse = false;
            if (_agent != null) { _agent.enabled = true; _agent.radius = 0.3f; }
            _sleepDuration = house.sleepDuration;
            _sleepTimer    = 0f;
            CurrentState   = VillagerState.GoingToSleep;
            SetAnim(true, false);
            _targetHouse?.OpenDoor();
        }

        private void UpdateGoingToSleep()
        {
            if (!_isEnteringHouse)
            {
                MoveTo(_doorThreshold);
                float distToDoor = Vector3.Distance(Flat(transform.position), Flat(_doorThreshold));
                if (distToDoor <= 1.2f)
                {
                    _isEnteringHouse = true;
                    if (_agent != null) _agent.enabled = false;
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, _sleepTarget, moveSpeed * 1.2f * Time.deltaTime);
                Vector3 dir = (_sleepTarget - transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                if (Vector3.Distance(transform.position, _sleepTarget) <= 0.2f)
                {
                    CurrentState = VillagerState.Sleeping;
                    _sleepTimer  = 0f;
                    SetAnim(false, false);
                    transform.position = _sleepTarget;
                    _targetHouse?.CloseDoor();
                }
            }
        }

        private void UpdateSleeping()
        {
            Energy = Mathf.Min(maxEnergy, Energy + sleepEnergyRestorePerSecond * Time.deltaTime);
            _sleepTimer += Time.deltaTime;
            if (_sleepTimer >= _sleepDuration || Energy >= maxEnergy)
            {
                var house = _targetHouse;
                house?.OpenDoor();
                house?.Vacate();
                _targetHouse = null;
                _onWakeUp?.Invoke();
                _onWakeUp = null;
                StartCoroutine(WalkOutAndIdle(house));
                if (house != null) StartCoroutine(CloseDoorDelayed(house));
            }
        }

        private System.Collections.IEnumerator WalkOutAndIdle(HouseController house)
        {
            CurrentState = VillagerState.Walking;
            if (_agent != null) _agent.enabled = true;
            Vector3 exitPos = transform.position - transform.forward * 2.5f;
            if (house != null)
            {
                Vector3 dirOut = (transform.position - house.transform.position).normalized;
                exitPos = transform.position + dirOut * 3.5f;
            }
            MoveTo(exitPos);
            SetAnim(true, false);
            float timeout = 3f;
            while (timeout > 0f && Vector3.Distance(transform.position, exitPos) > 0.8f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (_agent != null) _agent.radius = 0.35f;
            _sleepTarget = Vector3.zero;
            GoIdleDirect();
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

        // ── Raccolta strumenti ─────────────────────────────────────────

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

        public void WalkToPickaxePickup(PickaxePickup pickup)
        {
            if (pickup == null) return;
            _pickaxePickupTarget = pickup;
            CurrentState         = VillagerState.PickingUpPickaxe;
            SetAnim(true, false);
        }

        private void UpdatePickingUpPickaxe()
        {
            if (_pickaxePickupTarget == null) { GoIdle(); return; }
            Vector3 target = Flat(_pickaxePickupTarget.transform.position);
            MoveTo(target);
            if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance)
            {
                _pickaxePickupTarget.Collect(this);
                _pickaxePickupTarget = null;
                GoIdle();
            }
        }

        // ── Animator ──────────────────────────────────────────────────

        private void SetAnim(bool walking, bool chopping, bool mining = false)
        {
            if (_animator == null) return;
            if (walking && !_animator.GetBool(ParamWalking)) _animator.SetTrigger(ParamStartWalk);
            _animator.SetBool(ParamWalking,  walking);
            _animator.SetBool(ParamChopping, chopping);
            _animator.SetBool(ParamMining,   mining);
        }

        // ── Utilita' ───────────────────────────────────────────────────

        private void MoveTo(Vector3 target)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                if (!_agent.hasPath || _agent.pathPending || Vector3.Distance(_agent.destination, target) > 0.05f)
                    _agent.SetDestination(target);
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            Vector3 dir = target - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
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
            if (_targetResource != null) _targetResource.Release();
            if (_targetBench != null) _targetBench.Vacate();
            if (_targetHouse != null) _targetHouse.Vacate();
            _targetResource = null; _targetBench = null; _targetHouse = null;
            StopAgent();
            CurrentState = VillagerState.Dead;
            if (_animator != null) _animator.SetTrigger(ParamDying);
            Debug.Log($"[VillagerController] {name} is DEAD.");
        }

        public void Revive(float energyPercent)
        {
            if (CurrentState != VillagerState.Dead) return;
            Energy = maxEnergy * energyPercent;
            SetVisibility(true);
            GoIdleDirect();
            Debug.Log($"[VillagerController] {name} REVIVED.");
        }

        public void ForceIdle()
        {
            if (_targetResource != null) _targetResource.Release();
            if (_targetBench != null) _targetBench.Vacate();
            _targetResource = null; _targetDepot = null; _targetBench = null;
            GoIdleDirect();
        }

        public void PauseWork() { StopAgent(); }
        public void ResumeWork() { GoIdleDirect(); }

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
                VillagerState.MiningStone   => "Mining",
                VillagerState.CarryingStone => "Carrying Stone",
                VillagerState.GoingToSleep => "-> Sleep",
                VillagerState.Sleeping     => "Sleeping",
                VillagerState.PickingUpAxe => "-> Axe",
                VillagerState.PickingUpPickaxe => "-> Pickaxe",
                VillagerState.Resting      => "Resting",
                VillagerState.GoingToBench => "-> Bench",
                VillagerState.Sitting      => "Sitting",
                VillagerState.Dead         => "Dead",
                _                          => ""
            };
        }
    }
}
illagerState.Dead         => "Dead",
                _                          => ""
            };
        }
    }
}
