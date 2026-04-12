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
            Dead,
            Investigating, Gathering, Messenger,
            PickingUpCorpse, CarryingCorpse, Burying,
            Confronting
        }

        [Header("Personality")]
        public PersonalityData personality;

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
        public GameObject miningVFXPrefab; 
        private float _vfxTimer = 0f;

        [Header("Energia e Salute")]
        public float maxHealth                   = 100f;
        public float Health                      { get; private set; }
        public float maxEnergy                   = 100f;
        public float Energy               { get; private set; }
        public float energyDrainPerSecond        = 8f;
        public float exhaustionThreshold         = 20f;
        public float idleSleepThreshold          = 40f;   
        public float restDuration                = 8f;    
        public float restRestoreAmount           = 60f;   
        public float sleepEnergyRestorePerSecond = 15f;   

        public void ModifyHealth(float amount)
        {
            if (CurrentState == VillagerState.Dead) return;
            
            float oldHealth = Health;
            Health = Mathf.Clamp(Health + amount, 0f, maxHealth);

            // Visual feedback for damage/healing
            if (Mathf.Abs(Health - oldHealth) > 0.5f && FloatingTextSpawner.Instance != null)
            {
                string sign = amount > 0 ? "+" : "";
                Color color = amount > 0 ? Color.green : Color.red;
                FloatingTextSpawner.Instance.Spawn($"{sign}{amount:F0} HP", transform.position + Vector3.up * 2.5f, color);
            }

            if (Health <= 0)
            {
                Die();
            }
        }

        [Header("Social")]
        [Range(0, 100)]
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

            if (GetComponent<VillagerSocialReaction>() == null)
            {
                gameObject.AddComponent<VillagerSocialReaction>();
            }

            if (GetComponent<VillagerAuraController>() == null)
            {
                gameObject.AddComponent<VillagerAuraController>();
            }

            if (GetComponent<VillagerGroundEffectController>() == null)
            {
                gameObject.AddComponent<VillagerGroundEffectController>();
            }
if (GetComponent<VillagerAuraInfluence>() == null)
{
    gameObject.AddComponent<VillagerAuraInfluence>();
}

if (GetComponent<VillagerVision>() == null)
{
    gameObject.AddComponent<VillagerVision>();
}

if (_agent != null)
            if (GetComponent<VillagerEnergyBar>() == null)
            {
                gameObject.AddComponent<VillagerEnergyBar>();
            }

            if (_agent != null)
            {
                _agent.speed                 = moveSpeed;
                _agent.stoppingDistance      = waypointStopDistance;
                _agent.angularSpeed          = 360f;
                _agent.acceleration          = 10f;
                _agent.radius                = 0.4f; // Buffer increased (collider is 0.3)
                _agent.height                = 2.0f;
                _agent.baseOffset            = 0f; 
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                _agent.avoidancePriority     = Random.Range(40, 60);
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
            Health = maxHealth;

            // Inizializza personalità casuale
            if (personality == null || personality.primaryTrait == PersonalityTrait.Standard)
            {
                PersonalityTrait randomTrait = (PersonalityTrait)Random.Range(0, System.Enum.GetValues(typeof(PersonalityTrait)).Length);
                personality = new PersonalityData(randomTrait);
                Debug.Log($"[VillagerController] {name} nato con personalità: {personality.primaryTrait}");
            }

            GoIdleDirect();
        }

        private void Update()
        {
            if (CurrentState == VillagerState.Dead) return;

            UpdateAvoidancePriority();
            if (IsMovingState(CurrentState)) PredictiveAvoidance();

            // Update Animator based on actual NavMeshAgent velocity
            if (_agent != null && _agent.isOnNavMesh)
            {
                bool isActuallyMoving = _agent.velocity.sqrMagnitude > 0.01f;
                _animator.SetBool(ParamWalking, isActuallyMoving);
            }

            // Angel (Loyalty > 99) doesn't lose energy
            if (loyalty < 99f)
            {
                float drainMultiplier = 1f;
                if (loyalty >= 80f) drainMultiplier = 0.7f; // -30% drain
                
                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * drainMultiplier * Time.deltaTime * 0.1f); // Slow drain during generic state

                // Auto-death for exhaustion, unless Saint (Loyalty > 90)
                if (Energy <= 0 && loyalty < 90f)
                {
                    Die();
                    return;
                }
            }
            else
            {
                Energy = maxEnergy; // Angels always full
            }

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

                case VillagerState.Investigating:
                case VillagerState.Gathering:
                case VillagerState.Messenger:
                    // Gestito da VillagerSocialReaction
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
            // Se l'energia è bassa (anche se non ancora esausto), preferisce andare a riposare se non ha nulla da fare
            if (Energy < idleSleepThreshold)
            {
                Debug.Log($"[VillagerController] {name} è stanco (Energy: {Energy:F0}), cerca riposo preventivo.");
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
                
                // FORZA rotazione verso l'albero
                if (_targetResource != null)
                {
                    Vector3 dir = (_targetResource.transform.position - transform.position);
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                    }
                }

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

                // RUOTA verso la roccia mentre mina
                if (_targetResource != null)
                {
                    Vector3 dir = (_targetResource.transform.position - transform.position);
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                    }
                }

                _chopTimer += Time.deltaTime;
                _vfxTimer  += Time.deltaTime;
                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);

                // Spawn VFX every 0.8 seconds during mining
                if (miningVFXPrefab != null && _vfxTimer >= 0.8f)
                {
                    _vfxTimer = 0f;
                    Vector3 spawnPos = Vector3.Lerp(transform.position, _targetResource.transform.position, 0.7f) + Vector3.up * 0.5f;
                    GameObject vfx = Instantiate(miningVFXPrefab, spawnPos, Quaternion.LookRotation(transform.position - _targetResource.transform.position));
                    Destroy(vfx, 1.5f);
                }

                if (_chopTimer >= chopDuration)
                {
                    _vfxTimer = 0f;
                    int spaceLeft = 1;
                    if (rm != null) 
                    {
                        var d = rm.GetResourceData("Stone");
                        if (d != null) spaceLeft = d.currentMax - d.count;
                    }
                    _carriedStoneAmount = _targetResource.TakeResource(spaceLeft);

                    if (IsExhausted)
                    {
                        Debug.Log($"[VillagerController] {name} ha finito di minare ma è esausto. Va a dormire.");
                        if (_targetResource != null) _targetResource.Release();
                        _targetResource = null; _targetDepot = null;
                        StartAutoSleep();
                    }
                    else
                    {
                        GoCarryingStone();
                    }
                    return;
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
                // Dopo aver consegnato, controlliamo se siamo stanchi
                if (IsExhausted) StartAutoSleep(); else GoIdle();
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
                Debug.Log($"[VillagerController] {name} consegnato Stone. Energy attuale: {Energy:F1}");
                if (IsExhausted) StartAutoSleep(); else GoIdle();
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

        // ── Debug ──────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (_agent == null || !_agent.hasPath) return;

            var path = _agent.path;
            if (path.corners == null || path.corners.Length < 2) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                Gizmos.DrawSphere(path.corners[i + 1], 0.15f);
            }
        }

        // ── API pubblica ──────────────────────────────────────────────

        public void Die()
        {
            if (CurrentState == VillagerState.Dead) return;
            
            // IMMORTALITY: Saints (Loyalty > 90) cannot die!
            if (loyalty >= 90f)
            {
                Debug.Log($"[VillagerController] {name} is a SAINT (Loyalty: {loyalty:F0}) and REFUSES TO DIE!");
                if (FloatingTextSpawner.Instance != null)
                {
                    FloatingTextSpawner.Instance.Spawn("😇 IMMORTAL", transform.position + Vector3.up * 3.5f, Color.yellow);
                }
                return;
            }

            if (_targetResource != null) _targetResource.Release();
            if (_targetBench != null) _targetBench.Vacate();
            if (_targetHouse != null) _targetHouse.Vacate();
            _targetResource = null; _targetBench = null; _targetHouse = null;
            StopAgent();
            CurrentState = VillagerState.Dead;

            // Adjust collider to match fallen body
            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.direction = 2; // Z-axis (lying down forward)
                col.center = new Vector3(0, 0.2f, 1.2f); 
                col.height = 2.0f;
                col.radius = 0.5f;
            }

            if (_animator != null) _animator.SetTrigger(ParamDying);
            
            // Add Corpse lifecycle
            if (GetComponent<CorpseController>() == null)
            {
                gameObject.AddComponent<CorpseController>();
            }

            Debug.Log($"[VillagerController] {name} is DEAD.");
        }

        public void Revive(float energyPercent)
        {
            if (CurrentState != VillagerState.Dead) return;

            // Cleanup Corpse lifecycle
            var corpse = GetComponent<CorpseController>();
            if (corpse != null) corpse.CleanUp();

            // Reset collider to standing position
            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.direction = 1; // Y-axis (standing up)
                col.center = new Vector3(0, 1.0f, 0);
                col.height = 2.0f;
                col.radius = 0.3f;
            }

            // Force Animator back to default state (Idle)
            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f);
            }

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

        public void ModifyLoyalty(float amount)
        {
            if (CurrentState == VillagerState.Dead) return;
            float old = loyalty;
            loyalty = Mathf.Clamp(loyalty + amount, 0f, 100f);
            
            if (Mathf.Abs(loyalty - old) >= 0.5f && FloatingTextSpawner.Instance != null)
            {
                string sign = amount > 0 ? "+" : "";
                Color color = amount > 0 ? Color.green : Color.red;
                FloatingTextSpawner.Instance.Spawn($"{sign}{amount:F0} Loyalty", transform.position + Vector3.up * 3.0f, color);
            }
        }

        public void SetSocialState(VillagerState state)
        {
            if (CurrentState == VillagerState.Dead) return;
            CurrentState = state;
            
            // Basic animation handling for social states
            if (state == VillagerState.Investigating || state == VillagerState.Messenger)
                SetAnim(true, false);
            else
                SetAnim(false, false);
        }

        public void MoveToSocialTarget(Vector3 target, float speedMultiplier = 1.0f)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = moveSpeed * speedMultiplier;
                MoveTo(target);
            }
        }

        public void PauseWork() { StopAgent(); }
        public void ResumeWork() { _agent.speed = moveSpeed; GoIdleDirect(); }

        public void SetEnergy(float value)
        {
            Energy = Mathf.Clamp(value, 0f, maxEnergy);
        }

        private void UpdateAvoidancePriority()
        {
            if (_agent == null) return;

            // High priority (low number) for stationary/critical states
            // Low priority (high number) for moving states
            bool isStationary = CurrentState == VillagerState.Idle || 
                                CurrentState == VillagerState.ChoppingWood || 
                                CurrentState == VillagerState.MiningStone ||
                                CurrentState == VillagerState.Sitting ||
                                CurrentState == VillagerState.Sleeping ||
                                CurrentState == VillagerState.Resting;

            _agent.avoidancePriority = isStationary ? 30 : 50;
        }

        private void PredictiveAvoidance()
        {
            if (_agent == null || !_agent.isOnNavMesh || !_agent.hasPath) return;

            // Simple raycast/spherecast forward to detect imminent collisions
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Vector3 rayDir = _agent.velocity.normalized;
            if (rayDir.sqrMagnitude < 0.01f) rayDir = transform.forward;

            float checkDist = 2.0f;
            float checkRadius = 0.5f;
            
            // Mask for other villagers and default obstacles
            int mask = LayerMask.GetMask("Default"); 

            if (Physics.SphereCast(rayOrigin, checkRadius, rayDir, out RaycastHit hit, checkDist, mask))
            {
                if (hit.collider.gameObject == gameObject) return;

                // If something is in front, try to "nudge" the destination slightly to the side
                Vector3 cross = Vector3.Cross(Vector3.up, rayDir).normalized;
                
                // Decide side based on hit normal or just right
                Vector3 offset = cross * 1.5f;
                if (Vector3.Dot(hit.normal, cross) < 0) offset = -cross * 1.5f;

                Vector3 newTempDest = transform.position + rayDir * 2.0f + offset;

                if (NavMesh.SamplePosition(newTempDest, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    // Apply a slight steering force by updating destination if we are very close to a collision
                    if (hit.distance < 1.0f)
                    {
                        _agent.SetDestination(navHit.position);
                    }
                }
            }
        }

        private bool IsMovingState(VillagerState state)
        {
            return state == VillagerState.Walking || 
                   state == VillagerState.CarryingWood || 
                   state == VillagerState.CarryingStone ||
                   state == VillagerState.GoingToSleep ||
                   state == VillagerState.PickingUpAxe ||
                   state == VillagerState.PickingUpPickaxe ||
                   state == VillagerState.GoingToBench ||
                   state == VillagerState.Investigating ||
                   state == VillagerState.Messenger ||
                   state == VillagerState.CarryingCorpse;
        }

        public string GetStateLabel()
        {
            string traitPrefix = personality != null ? $"[{personality.primaryTrait}] " : "[Standard] ";
            string loyaltyText = $" | Loyalty: {Mathf.RoundToInt(loyalty)}";
            string stateName = CurrentState switch
            {
                VillagerState.Idle         => "Idle",
                VillagerState.Walking      => "Walking Around",
                VillagerState.ChoppingWood => "Chopping Wood",
                VillagerState.CarryingWood => "Carrying Wood",
                VillagerState.MiningStone   => "Mining Stone",
                VillagerState.CarryingStone => "Carrying Stone",
                VillagerState.GoingToSleep => "Going to Sleep",
                VillagerState.Sleeping     => "Zzz...",
                VillagerState.PickingUpAxe => "Getting Axe",
                VillagerState.PickingUpPickaxe => "Getting Pickaxe",
                VillagerState.Resting      => "Resting",
                VillagerState.GoingToBench => "Going to Bench",
                VillagerState.Sitting      => "Sitting on Bench",
                VillagerState.Dead         => "Dead",
                VillagerState.Investigating => "Investigating",
                VillagerState.Gathering    => "Gathering",
                VillagerState.Messenger    => "Spreading News",
                VillagerState.PickingUpCorpse => "Picking up Corpse",
                VillagerState.CarryingCorpse  => "Carrying Dead Friend",
                VillagerState.Burying         => "Burying...",
                _                          => "Unknown"
            };
            return traitPrefix + stateName + loyaltyText;
        }
    }
}
