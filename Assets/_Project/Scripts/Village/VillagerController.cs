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
            Confronting,
            HeadingToPickup
        }

        [Header("Personality")]
        public PersonalityData personality;

        [Header("Movement")]
        public float moveSpeed            = 1.5f; 
        public float waypointStopDistance = 0.3f;
        public float wanderRadius         = 10f;  

        [Header("Idle")]
        public float idleMinDuration      = 2f;
        public float idleMaxDuration      = 4f;

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
            float oldHealth = Health; Health = Mathf.Clamp(Health + amount, 0f, maxHealth);
            if (Mathf.Abs(Health - oldHealth) > 0.5f && FloatingTextSpawner.Instance != null)
            { string sign = amount > 0 ? "+" : ""; Color color = amount > 0 ? Color.green : Color.red;
              FloatingTextSpawner.Instance.Spawn($"{sign}{amount:F0} HP", transform.position + Vector3.up * 2.5f, color); }
            if (Health <= 0) Die();
        }

        [Header("Social")]
        [Range(0, 100)]
        public float loyalty = 50f; 
        public float perceptionRadius = 20f;

        public VillagerState CurrentState { get; private set; } = VillagerState.Idle;
        public bool  IsExhausted          => Energy <= exhaustionThreshold;
        public bool  HasPersonalAxe       { get; set; } = false;
        public bool  HasPersonalPickaxe   { get; set; } = false;

        private NavMeshAgent _agent;
        private Animator _animator;
        private static readonly int ParamWalking   = Animator.StringToHash("isWalking");
        private static readonly int ParamChopping  = Animator.StringToHash("isChopping");
        private static readonly int ParamMining    = Animator.StringToHash("isMining");
        private static readonly int ParamDying     = Animator.StringToHash("dying");
        private static readonly int ParamStartWalk = Animator.StringToHash("startWalk");

        private Vector3 _walkTarget  = Vector3.zero;
        private float   _idleTimer   = 0f;
        private float   _idleDuration = 0f;

        private ResourceNode           _targetResource;
        private ResourcePickup         _targetPickup;
        private GenericDepotController _targetDepot;      
        private bool  _arrivedAtNode = false;
        private float _chopTimer     = 0f;
        private int   _carriedWoodAmount = 0;
        private int   _carriedStoneAmount = 0;
        private GameObject _carriedVisual;
        private float _deliveryStuckTimer = 0f;

        private Vector3          _sleepTarget;
        private Vector3          _doorThreshold;
        private bool             _isEnteringHouse;
        private float            _sleepDuration;
        private float            _sleepTimer;
        private HouseController  _targetHouse;

        private float _restTimer = 0f;
        private BenchController _targetBench;
        private float           _sitTimer;

        private AxePickup     _axePickupTarget;
        private PickaxePickup _pickaxePickupTarget;
        private float         _pickupTimeoutTimer = 0f;

        private void Start()
        {
            _animator = GetComponent<Animator>(); _agent = GetComponent<NavMeshAgent>();
            var rb = GetComponent<Rigidbody>(); if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            if (GetComponent<VillagerSocialReaction>() == null) gameObject.AddComponent<VillagerSocialReaction>();
            if (GetComponent<VillagerAuraController>() == null) gameObject.AddComponent<VillagerAuraController>();
            if (GetComponent<VillagerGroundEffectController>() == null) gameObject.AddComponent<VillagerGroundEffectController>();
            if (GetComponent<VillagerAuraInfluence>() == null) gameObject.AddComponent<VillagerAuraInfluence>();
            if (GetComponent<VillagerVision>() == null) gameObject.AddComponent<VillagerVision>();
            if (GetComponent<VillagerEnergyBar>() == null) gameObject.AddComponent<VillagerEnergyBar>();

            if (_agent != null) { 
                _agent.speed = moveSpeed; _agent.stoppingDistance = waypointStopDistance; 
                _agent.angularSpeed = 360f; _agent.acceleration = 10f; _agent.radius = 0.4f; 
                _agent.height = 2.0f; _agent.baseOffset = 0f; 
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; 
                _agent.avoidancePriority = Random.Range(40, 60); 
            }
            var col = GetComponent<CapsuleCollider>(); 
            if (col != null) { col.center = new Vector3(0, 1.0f, 0); col.radius = 0.3f; col.height = 2.0f; col.isTrigger = false; }

            Energy = maxEnergy; Health = maxHealth;
            if (personality == null || personality.primaryTrait == PersonalityTrait.Standard) { 
                PersonalityTrait randomTrait = (PersonalityTrait)Random.Range(0, System.Enum.GetValues(typeof(PersonalityTrait)).Length); 
                personality = new PersonalityData(randomTrait); 
            }
            GoIdleDirect();
        }

        private void Update()
        {
            if (CurrentState == VillagerState.Dead) { ClearCarriedVisual(); return; }
            UpdateAvoidancePriority(); if (IsMovingState(CurrentState)) PredictiveAvoidance();
            
            if (_agent != null && _agent.isOnNavMesh) {
                bool isActuallyMoving = _agent.velocity.sqrMagnitude > 0.05f;
                _animator.SetBool(ParamWalking, isActuallyMoving);
            }

            if (loyalty < 99f) { 
                float drainMultiplier = (loyalty >= 80f) ? 0.7f : 1f; 
                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * drainMultiplier * Time.deltaTime * 0.1f); 
                if (Energy <= 0 && loyalty < 90f) { Die(); return; } 
            } else Energy = maxEnergy;

            switch (CurrentState) {
                case VillagerState.Idle: _idleTimer += Time.deltaTime; if (_idleTimer >= _idleDuration) GoWalking(); CheckForPickups(); break;
                case VillagerState.Walking: UpdateWalking(); CheckForPickups(); break;
                case VillagerState.HeadingToPickup: UpdateHeadingToPickup(); break;
                case VillagerState.ChoppingWood: UpdateChoppingWood(); break;
                case VillagerState.CarryingWood: UpdateCarryingWood(); break;
                case VillagerState.MiningStone: UpdateMiningStone(); break;
                case VillagerState.CarryingStone: UpdateCarryingStone(); break;
                case VillagerState.GoingToSleep: UpdateGoingToSleep(); break;
                case VillagerState.Sleeping: UpdateSleeping(); break;
                case VillagerState.PickingUpAxe: UpdatePickingUpAxe(); break;
                case VillagerState.PickingUpPickaxe: UpdatePickingUpPickaxe(); break;
                case VillagerState.Resting: UpdateResting(); break;
                case VillagerState.GoingToBench: UpdateGoingToBench(); break;
                case VillagerState.Sitting: UpdateSitting(); break;
            }
        }

        private void CheckForPickups()
        {
            if (IsExhausted || _carriedWoodAmount > 0 || _carriedStoneAmount > 0) return;
            Collider[] hits = Physics.OverlapSphere(transform.position, perceptionRadius);
            foreach (var hit in hits) {
                var pickup = hit.GetComponent<ResourcePickup>();
                if (pickup != null && pickup.CanBeClaimed()) {
                    if (pickup.Claim(this)) {
                        _targetPickup = pickup; _pickupTimeoutTimer = 0f;
                        CurrentState = VillagerState.HeadingToPickup; 
                        MoveTo(pickup.transform.position);
                        SetAnim(true, false); break;
                    }
                }
            }
        }

        private void UpdateHeadingToPickup()
        {
            if (_targetPickup == null) { GoIdle(); return; }
            _pickupTimeoutTimer += Time.deltaTime;
            if (_pickupTimeoutTimer > 8f) { _targetPickup.Unclaim(); _targetPickup = null; GoIdle(); return; }
            if (Time.frameCount % 30 == 0) MoveTo(_targetPickup.transform.position);
            if (AgentReachedTarget(_targetPickup.transform.position, 1.2f)) {
                if (_targetPickup.Collect(this)) { _targetPickup = null; }
                else { _targetPickup.Unclaim(); _targetPickup = null; GoIdle(); }
            }
        }

        public void ReceiveResource(string type, int amount)
        { 
            if (type == "Wood") { _carriedWoodAmount = amount; UpdateCarriedVisual(new Color(0.5f, 0.25f, 0f)); GoCarryingWood(); }
            else if (type == "Stone") { _carriedStoneAmount = amount; UpdateCarriedVisual(Color.gray); GoCarryingStone(); } 
        }

        private void UpdateCarriedVisual(Color color)
        { 
            if (_carriedVisual == null) { 
                _carriedVisual = GameObject.CreatePrimitive(PrimitiveType.Cube); 
                Destroy(_carriedVisual.GetComponent<BoxCollider>()); 
                _carriedVisual.transform.SetParent(transform); 
                _carriedVisual.transform.localPosition = new Vector3(0, 2.2f, 0); 
                _carriedVisual.transform.localScale = Vector3.one * 0.3f; 
            }
            var rend = _carriedVisual.GetComponent<Renderer>(); if (rend != null) rend.material.color = color; 
        }

        private void ClearCarriedVisual() { if (_carriedVisual != null) { Destroy(_carriedVisual); _carriedVisual = null; } }

        public void GoIdleDirect() { StopAgent(); CurrentState = VillagerState.Idle; _idleDuration = Random.Range(idleMinDuration, idleMaxDuration); _idleTimer = 0f; SetAnim(false, false); ClearCarriedVisual(); if (_targetPickup != null) { _targetPickup.Unclaim(); _targetPickup = null; } }
        private void GoIdle() { if (Energy < idleSleepThreshold) { StartAutoSleep(); return; } GoIdleDirect(); }
        private void GoWalking() { if (!TryGetRandomNavMeshPoint(out Vector3 dest)) { GoIdleDirect(); return; } _walkTarget = dest; CurrentState = VillagerState.Walking; SetAnim(true, false); MoveTo(Flat(dest)); }
        private void UpdateWalking() { bool arrived; if (_agent != null && _agent.isOnNavMesh) arrived = !_agent.pathPending && _agent.remainingDistance <= waypointStopDistance; else arrived = Vector3.Distance(Flat(transform.position), Flat(_walkTarget)) <= waypointStopDistance; if (arrived) GoIdle(); }
        private bool TryGetRandomNavMeshPoint(out Vector3 result) { Vector3 randomDir = Random.insideUnitSphere * wanderRadius; randomDir.y = 0f; randomDir += transform.position; if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas)) { result = hit.position; return true; } result = transform.position; return false; }

        public void AssignResourceTask(ResourceNode node) { if (node == null || !node.TryAssign(this)) return; _targetResource = node; _arrivedAtNode = false; _chopTimer = 0f; _targetDepot = FindNearestDepot(); CurrentState = (node.resourceName == "Wood") ? VillagerState.ChoppingWood : VillagerState.MiningStone; }

        private void UpdateChoppingWood()
        {
            if (_targetResource == null) { GoIdle(); return; }
            var rm = ResourceManager.Instance;
            if (rm != null) { var woodData = rm.GetResourceData("Wood"); if (woodData != null && woodData.count >= woodData.currentMax) { _targetResource.Release(); _targetResource = null; _targetDepot = null; GoIdleDirect(); return; } }
            Vector3 nodePos = Flat(_targetResource.transform.position);
            if (!_arrivedAtNode) { MoveTo(nodePos); SetAnim(true, false); if (AgentReachedTarget(nodePos, taskStopDistance)) { _arrivedAtNode = true; StopAgent(); } }
            else { SetAnim(false, true); if (_targetResource != null) { Vector3 dir = (_targetResource.transform.position - transform.position); dir.y = 0; if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f); }
                _chopTimer += Time.deltaTime; Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);
                if (_chopTimer >= chopDuration) { int spaceLeft = rm != null ? rm.GetResourceData("Wood").currentMax - rm.GetResourceData("Wood").count : 1; _carriedWoodAmount = _targetResource.TakeResource(spaceLeft); UpdateCarriedVisual(new Color(0.5f, 0.25f, 0f)); GoCarryingWood(); return; }
                if (IsExhausted) { if (_targetResource != null) _targetResource.Release(); _targetResource = null; _targetDepot = null; StartAutoSleep(); } }
        }

        private void UpdateMiningStone()
        {
            if (_targetResource == null) { GoIdle(); return; }
            var rm = ResourceManager.Instance;
            if (rm != null) { var stoneData = rm.GetResourceData("Stone"); if (stoneData != null && stoneData.count >= stoneData.currentMax) { _targetResource.Release(); _targetResource = null; _targetDepot = null; GoIdleDirect(); return; } }
            
            // Calcola una posizione sicura ESTERNA alla roccia
            Vector3 dirFromNode = (transform.position - _targetResource.transform.position);
            dirFromNode.y = 0;
            if (dirFromNode.sqrMagnitude < 0.1f) dirFromNode = Vector3.forward;
            Vector3 safePos = Flat(_targetResource.transform.position) + dirFromNode.normalized * 2.0f;

            if (!_arrivedAtNode) { 
                if (_agent != null) {
                    _agent.stoppingDistance = 0.5f; // Stop distance vicino alla safePos
                    _agent.updateRotation = true;
                }
                MoveTo(safePos); 
                SetAnim(true, false, false); 
                if (AgentReachedTarget(safePos, 0.8f)) { 
                    _arrivedAtNode = true; 
                    StopAgent(); 
                } 
            }
            else { 
                SetAnim(false, false, true); 
                if (_agent != null) _agent.updateRotation = false; // Forza stop rotazione agent

                if (_targetResource != null) { 
                    // Rotazione FORZATA verso la roccia ogni frame
                    Vector3 dir = (_targetResource.transform.position - transform.position); 
                    dir.y = 0; 
                    if (dir.sqrMagnitude > 0.001f) {
                        transform.rotation = Quaternion.LookRotation(dir);
                    }
                }
                
                _chopTimer += Time.deltaTime; 
                _vfxTimer += Time.deltaTime; 
                Energy = Mathf.Max(0f, Energy - energyDrainPerSecond * Time.deltaTime);
                
                if (miningVFXPrefab != null && _vfxTimer >= 0.8f) { 
                    _vfxTimer = 0f; 
                    Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.8f; 
                    GameObject vfx = Instantiate(miningVFXPrefab, spawnPos, transform.rotation); 
                    Destroy(vfx, 1.5f); 
                }
                
                if (_chopTimer >= chopDuration) { 
                    if (_agent != null) {
                        _agent.updateRotation = true; 
                        _agent.stoppingDistance = waypointStopDistance;
                    }
                    _vfxTimer = 0f; 
                    int spaceLeft = rm != null ? rm.GetResourceData("Stone").currentMax - rm.GetResourceData("Stone").count : 1; 
                    _carriedStoneAmount = _targetResource.TakeResource(spaceLeft); 
                    UpdateCarriedVisual(Color.gray); 
                    if (IsExhausted) { if (_targetResource != null) _targetResource.Release(); _targetResource = null; _targetDepot = null; StartAutoSleep(); } 
                    else GoCarryingStone(); 
                    return; 
                } 
            }
        }

        private void GoCarryingWood() { _targetDepot = FindNearestDepot(); CurrentState = VillagerState.CarryingWood; if (_targetDepot != null) { if (_agent != null) _agent.stoppingDistance = 2.0f; MoveTo(_targetDepot.GetDeliveryPosition()); } SetAnim(true, false); }
        private void GoCarryingStone() { _targetDepot = FindNearestDepot(); CurrentState = VillagerState.CarryingStone; if (_targetDepot != null) { if (_agent != null) _agent.stoppingDistance = 2.0f; MoveTo(_targetDepot.GetDeliveryPosition()); } SetAnim(true, false, false); }

        private void UpdateCarryingWood()
        { 
            if (_targetDepot == null) { _targetDepot = FindNearestDepot(); if (_targetDepot == null) { ResourceManager.Instance?.AddResource("Wood", _carriedWoodAmount); _carriedWoodAmount = 0; ClearCarriedVisual(); GoIdleDirect(); return; } }
            Vector3 depotPos = _targetDepot.GetDeliveryPosition(); 
            MoveTo(depotPos); 
            float dist = Vector3.Distance(Flat(transform.position), Flat(depotPos));
            // Logica consegna "Bump-Proof": se sono molto vicino (3m), scarico comunque
            if (dist < 3.0f || AgentReachedTarget(depotPos, 2.5f)) { 
                ResourceManager.Instance?.AddResource("Wood", _carriedWoodAmount); 
                _carriedWoodAmount = 0; _targetResource = null; _targetDepot = null; ClearCarriedVisual(); 
                if (IsExhausted) StartAutoSleep(); else GoIdle(); 
            } 
        }

        private void UpdateCarryingStone()
        { 
            if (_targetDepot == null) { _targetDepot = FindNearestDepot(); if (_targetDepot == null) { ResourceManager.Instance?.AddResource("Stone", _carriedStoneAmount); _carriedStoneAmount = 0; ClearCarriedVisual(); GoIdleDirect(); return; } }
            Vector3 depotPos = _targetDepot.GetDeliveryPosition(); 
            MoveTo(depotPos); 
            float dist = Vector3.Distance(Flat(transform.position), Flat(depotPos));
            // Logica consegna "Bump-Proof"
            if (dist < 3.0f || AgentReachedTarget(depotPos, 2.5f)) { 
                ResourceManager.Instance?.AddResource("Stone", _carriedStoneAmount); 
                _carriedStoneAmount = 0; _targetResource = null; _targetDepot = null; ClearCarriedVisual(); 
                if (IsExhausted) StartAutoSleep(); else GoIdle(); 
            } 
        }

        private GenericDepotController FindNearestDepot() { GenericDepotController best = null; float minDist = float.MaxValue; foreach (var d in FindObjectsOfType<GenericDepotController>()) { float dist = Vector3.Distance(transform.position, d.transform.position); if (dist < minDist) { minDist = dist; best = d; } } return best; }
        private void StartAutoSleep() { var bench = FindFreeBench(); if (bench != null) { GoToBench(bench); return; } var house = FindFreeHouse(); if (house != null) { GoToHouseAndSleep(house); return; } GoResting(); }
        private HouseController FindFreeHouse() { HouseController best = null; float minDist = float.MaxValue; foreach (var h in FindObjectsOfType<HouseController>()) { if (h.IsFull) continue; float d = Vector3.Distance(transform.position, h.transform.position); if (d < minDist) { minDist = d; best = h; } } return best; }
        private BenchController FindFreeBench() { BenchController best = null; float minDist = float.MaxValue; foreach (var b in FindObjectsOfType<BenchController>()) { if (b.IsOccupied) continue; float d = Vector3.Distance(transform.position, b.transform.position); if (d < minDist) { minDist = d; best = b; } } return best; }
        public void GoToBench(BenchController bench) { if (bench == null || !bench.TryOccupy(this)) return; if (_targetResource != null) _targetResource.Release(); _targetResource = null; _targetDepot = null; _targetBench = bench; _sitTimer = 0f; CurrentState = VillagerState.GoingToBench; SetAnim(true, false); }
        private void UpdateGoingToBench() { if (_targetBench == null) { GoIdleDirect(); return; } Vector3 target = Flat(_targetBench.GetSitPosition()); MoveTo(target); if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance) { StopAgent(); CurrentState = VillagerState.Sitting; _sitTimer = 0f; SetAnim(false, false); } }
        private void UpdateSitting() { _sitTimer += Time.deltaTime; Energy = Mathf.Min(maxEnergy, Energy + _targetBench.energyRecoveryPerSecond * Time.deltaTime); if (_sitTimer >= _targetBench.sitDuration || Energy >= maxEnergy) { _targetBench.Vacate(); _targetBench = null; GoIdleDirect(); } }
        private void GoResting() { StopAgent(); CurrentState = VillagerState.Resting; _restTimer = 0f; SetAnim(false, false); }
        private void UpdateResting() { _restTimer += Time.deltaTime; Energy = Mathf.Min(maxEnergy, Energy + (restRestoreAmount / restDuration) * Time.deltaTime); if (_restTimer >= restDuration || Energy >= maxEnergy) GoIdleDirect(); }
        private void GoToHouseAndSleep(HouseController house) { if (!house.TryOccupy()) { GoResting(); return; } SetAnim(false, false); if (_targetResource != null) _targetResource.Release(); _targetResource = null; _targetDepot = null; _targetHouse = house; _doorThreshold = house.GetDoorThreshold(); _sleepTarget = house.GetSleepPosition(); _isEnteringHouse = false; if (_agent != null) { _agent.enabled = true; _agent.radius = 0.3f; } _sleepDuration = house.sleepDuration; _sleepTimer = 0f; CurrentState = VillagerState.GoingToSleep; SetAnim(true, false); _targetHouse?.OpenDoor(); }
        private void UpdateGoingToSleep() { if (!_isEnteringHouse) { MoveTo(_doorThreshold); if (Vector3.Distance(Flat(transform.position), Flat(_doorThreshold)) <= 1.2f) { _isEnteringHouse = true; if (_agent != null) _agent.enabled = false; } }
            else { transform.position = Vector3.MoveTowards(transform.position, _sleepTarget, moveSpeed * 1.2f * Time.deltaTime); Vector3 dir = (_sleepTarget - transform.position).normalized; dir.y = 0f; if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime); if (Vector3.Distance(transform.position, _sleepTarget) <= 0.2f) { CurrentState = VillagerState.Sleeping; _sleepTimer = 0f; SetAnim(false, false); transform.position = _sleepTarget; _targetHouse?.CloseDoor(); } } }
        private void UpdateSleeping() { Energy = Mathf.Min(maxEnergy, Energy + sleepEnergyRestorePerSecond * Time.deltaTime); _sleepTimer += Time.deltaTime; if (_sleepTimer >= _sleepDuration || Energy >= maxEnergy) { var house = _targetHouse; house?.OpenDoor(); house?.Vacate(); _targetHouse = null; StartCoroutine(WalkOutAndIdle(house)); if (house != null) StartCoroutine(CloseDoorDelayed(house)); } }
        private System.Collections.IEnumerator WalkOutAndIdle(HouseController house) { CurrentState = VillagerState.Walking; if (_agent != null) _agent.enabled = true; Vector3 exitPos = transform.position - transform.forward * 2.5f; if (house != null) exitPos = transform.position + (transform.position - house.transform.position).normalized * 3.5f; MoveTo(exitPos); SetAnim(true, false); float timeout = 3f; while (timeout > 0f && Vector3.Distance(transform.position, exitPos) > 0.8f) { timeout -= Time.deltaTime; yield return null; } if (_agent != null) _agent.radius = 0.35f; _sleepTarget = Vector3.zero; GoIdleDirect(); }
        private System.Collections.IEnumerator CloseDoorDelayed(HouseController house) { yield return new WaitForSeconds(1.5f); house.CloseDoor(); }
        private void SetVisibility(bool visible) { foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = visible; }
        
        public void WalkToAxePickup(AxePickup pickup) { if (pickup == null) return; _axePickupTarget = pickup; CurrentState = VillagerState.PickingUpAxe; SetAnim(true, false); }
        private void UpdatePickingUpAxe() { if (_axePickupTarget == null) { GoIdle(); return; } Vector3 target = Flat(_axePickupTarget.transform.position); MoveTo(target); if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance) { _axePickupTarget.Collect(this); _axePickupTarget = null; GoIdle(); } }
        public void WalkToPickaxePickup(PickaxePickup pickup) { if (pickup == null) return; _pickaxePickupTarget = pickup; CurrentState = VillagerState.PickingUpPickaxe; SetAnim(true, false); }
        private void UpdatePickingUpPickaxe() { if (_pickaxePickupTarget == null) { GoIdle(); return; } Vector3 target = Flat(_pickaxePickupTarget.transform.position); MoveTo(target); if (Vector3.Distance(Flat(transform.position), target) <= taskStopDistance) { _pickaxePickupTarget.Collect(this); _pickaxePickupTarget = null; GoIdle(); } }
        
        private void SetAnim(bool walking, bool chopping, bool mining = false) { if (_animator == null) return; if (walking && !_animator.GetBool(ParamWalking)) _animator.SetTrigger(ParamStartWalk); _animator.SetBool(ParamWalking, walking); _animator.SetBool(ParamChopping, chopping); _animator.SetBool(ParamMining, mining); }
        private void MoveTo(Vector3 target) { if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = false; if (!_agent.hasPath || _agent.pathPending || Vector3.Distance(_agent.destination, target) > 0.05f) _agent.SetDestination(target); return; } transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime); Vector3 dir = target - transform.position; dir.y = 0f; if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime); }
        private void StopAgent() { if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); } }
        private bool AgentReachedTarget(Vector3 target, float stopDist) { if (_agent != null && _agent.isOnNavMesh && !_agent.pathPending) return _agent.remainingDistance <= stopDist || Vector3.Distance(Flat(transform.position), Flat(target)) <= stopDist; return Vector3.Distance(Flat(transform.position), Flat(target)) <= stopDist; }
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        
        public void Die() { 
            if (CurrentState == VillagerState.Dead) return; 

            // Regola: se Loyalty > 80, il villager sopravvive allo Smite
            if (loyalty > 80f) { 
                if (personality?.primaryTrait == PersonalityTrait.Courageous || personality?.primaryTrait == PersonalityTrait.Standard) {
                    ModifyLoyalty(-50f); // Calo drastico della lealtà
                    if (FloatingTextSpawner.Instance != null) 
                        FloatingTextSpawner.Instance.Spawn("💢 DIVINE BETRAYAL!", transform.position + Vector3.up * 3.5f, Color.red);
                } else {
                    if (FloatingTextSpawner.Instance != null) 
                        FloatingTextSpawner.Instance.Spawn("😇 PROTECTED", transform.position + Vector3.up * 3.5f, Color.yellow);
                }
                return; // Non muore
            }

            if (_targetResource != null) _targetResource.Release(); 
            if (_targetBench != null) _targetBench.Vacate(); 
            if (_targetHouse != null) _targetHouse.Vacate(); 
            _targetResource = null; _targetBench = null; _targetHouse = null; StopAgent(); 
            
            // --- DROP RESOURCES ON DEATH ---
            DropCarriedResources();

            CurrentState = VillagerState.Dead; var col = GetComponent<CapsuleCollider>(); if (col != null) { col.direction = 2; col.center = new Vector3(0, 0.2f, 1.2f); col.height = 2.0f; col.radius = 0.5f; } if (_animator != null) { _animator.SetTrigger(ParamDying); _animator.Update(0); } if (GetComponent<CorpseController>() == null) gameObject.AddComponent<CorpseController>(); ClearCarriedVisual(); if (_targetPickup != null) { _targetPickup.Unclaim(); _targetPickup = null; } }

        private void DropCarriedResources()
        {
            int woodToDrop = _carriedWoodAmount;
            int stoneToDrop = _carriedStoneAmount;

            if (woodToDrop > 0) SpawnResourcePickups("Wood", woodToDrop, new Color(0.5f, 0.25f, 0f));
            if (stoneToDrop > 0) SpawnResourcePickups("Stone", stoneToDrop, Color.gray);

            _carriedWoodAmount = 0;
            _carriedStoneAmount = 0;
        }

        private void SpawnResourcePickups(string type, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = spawnPos;
                cube.transform.localScale = Vector3.one * 0.4f;
                
                var rb = cube.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                
                var rend = cube.GetComponent<Renderer>();
                if (rend != null) rend.material.color = color;

                var pickup = cube.AddComponent<ResourcePickup>();
                pickup.Initialize(type, 1);
            }
        }
        public void Revive(float energyPercent) { if (CurrentState != VillagerState.Dead) return; var corpse = GetComponent<CorpseController>(); if (corpse != null) corpse.CleanUp(); var col = GetComponent<CapsuleCollider>(); if (col != null) { col.direction = 1; col.center = new Vector3(0, 1.0f, 0); col.height = 2.0f; col.radius = 0.3f; } if (_animator != null) { _animator.Rebind(); _animator.Update(0f); } Energy = maxEnergy * energyPercent; SetVisibility(true); GoIdleDirect(); }
        public void ForceIdle() { if (_targetResource != null) _targetResource.Release(); if (_targetBench != null) _targetBench.Vacate(); _targetResource = null; _targetDepot = null; _targetBench = null; GoIdleDirect(); }
        public void ModifyLoyalty(float amount) { if (CurrentState == VillagerState.Dead) return; float old = loyalty; loyalty = Mathf.Clamp(loyalty + amount, 0f, 100f); if (Mathf.Abs(loyalty - old) >= 0.5f && FloatingTextSpawner.Instance != null) { string sign = amount > 0 ? "+" : ""; Color color = amount > 0 ? Color.green : Color.red; FloatingTextSpawner.Instance.Spawn($"{sign}{amount:F0} Loyalty", transform.position + Vector3.up * 3.0f, color); } }
        public void SetSocialState(VillagerState state) { if (CurrentState == VillagerState.Dead) return; CurrentState = state; if (state == VillagerState.Investigating || state == VillagerState.Messenger) SetAnim(true, false); else SetAnim(false, false); }
        public void MoveToSocialTarget(Vector3 target, float speedMultiplier = 1.0f) { if (_agent != null && _agent.isOnNavMesh) { _agent.speed = moveSpeed * speedMultiplier; MoveTo(target); } }
        public void PauseWork() { StopAgent(); }
        public void ResumeWork() { if (_agent != null) _agent.speed = moveSpeed; GoIdleDirect(); }
        public void SetEnergy(float value) { Energy = Mathf.Clamp(value, 0f, maxEnergy); }
        private void UpdateAvoidancePriority() { if (_agent == null) return; bool isStationary = CurrentState == VillagerState.Idle || CurrentState == VillagerState.ChoppingWood || CurrentState == VillagerState.MiningStone || CurrentState == VillagerState.Sitting || CurrentState == VillagerState.Sleeping || CurrentState == VillagerState.Resting; _agent.avoidancePriority = isStationary ? 30 : 50; }
        private void PredictiveAvoidance() { if (_agent == null || !_agent.isOnNavMesh || !_agent.hasPath) return; Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; Vector3 rayDir = _agent.velocity.normalized; if (rayDir.sqrMagnitude < 0.01f) rayDir = transform.forward; float checkDist = 2.0f; float checkRadius = 0.5f; int mask = LayerMask.GetMask("Default"); if (Physics.SphereCast(rayOrigin, checkRadius, rayDir, out RaycastHit hit, checkDist, mask)) { if (hit.collider.gameObject == gameObject) return; Vector3 cross = Vector3.Cross(Vector3.up, rayDir).normalized; Vector3 offset = cross * 1.5f; if (Vector3.Dot(hit.normal, cross) < 0) offset = -cross * 1.5f; Vector3 newTempDest = transform.position + rayDir * 2.0f + offset; if (NavMesh.SamplePosition(newTempDest, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas)) { if (hit.distance < 1.0f) _agent.SetDestination(navHit.position); } } }
        private bool IsMovingState(VillagerState state) { return state == VillagerState.Walking || state == VillagerState.CarryingWood || state == VillagerState.CarryingStone || state == VillagerState.GoingToSleep || state == VillagerState.PickingUpAxe || state == VillagerState.PickingUpPickaxe || state == VillagerState.GoingToBench || state == VillagerState.Investigating || state == VillagerState.Messenger || state == VillagerState.CarryingCorpse || state == VillagerState.HeadingToPickup; }
        public string GetStateLabel() { string traitPrefix = personality != null ? $"[{personality.primaryTrait}] " : "[Standard] "; string loyaltyText = $" | Loyalty: {Mathf.RoundToInt(loyalty)}"; string stateName = CurrentState switch { VillagerState.Idle => "Idle", VillagerState.Walking => "Walking Around", VillagerState.ChoppingWood => "Chopping Wood", VillagerState.CarryingWood => "Carrying Wood", VillagerState.MiningStone => "Mining Stone", VillagerState.CarryingStone => "Carrying Stone", VillagerState.GoingToSleep => "Going to Sleep", VillagerState.Sleeping => "Zzz...", VillagerState.PickingUpAxe => "Getting Axe", VillagerState.PickingUpPickaxe => "Getting Pickaxe", VillagerState.Resting => "Resting", VillagerState.GoingToBench => "Going to Bench", VillagerState.Sitting => "Sitting on Bench", VillagerState.Dead => "Dead", VillagerState.Investigating => "Investigating", VillagerState.Gathering => "Gathering", VillagerState.Messenger => "Spreading News", VillagerState.PickingUpCorpse => "Picking up Corpse", VillagerState.CarryingCorpse => "Carrying Dead Friend", VillagerState.Burying => "Burying...", VillagerState.HeadingToPickup => "Heading to Resource", _ => "Unknown" }; return traitPrefix + stateName + loyaltyText; }
    }
}
