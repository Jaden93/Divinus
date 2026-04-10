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
        public float moveSpeed            = 1.5f; // Ridotto da 2.0 per evitare effetto pattinaggio
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
        public float exhaustionThreshold         = 20f;
        public float idleSleepThreshold          = 40f;   // va a riposare se idle con energia < questa soglia
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
        private static readonly int ParamDying     = Animator.StringToHash("dying");

        // ── Stato interno walking ──────────────────────────────────────
        private Vector3 _walkTarget  = Vector3.zero;
        private float   _idleTimer   = 0f;
        private float   _idleDuration = 0f;

        // ── Stato interno task ─────────────────────────────────────────
        private ResourceNode        _targetResource;
        private WoodDepotController _targetDepotCtrl;   // depot fisico (può essere null)
        private bool  _arrivedAtNode = false;
        private float _chopTimer     = 0f;
        private int   _carriedWoodAmount = 0;

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

        // ── Stato interno raccolta ascia ───────────────────────────────
        private AxePickup _axePickupTarget;

        // ──────────────────────────────────────────────────────────────

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            // Configura Rigidbody per evitare che la fisica "spinga" il villager nel terreno
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; 
            rb.useGravity = false;

            // Auto-configure NavMeshAgent
            if (_agent != null)
            {
                _agent.speed                 = moveSpeed;
                _agent.stoppingDistance      = waypointStopDistance;
                _agent.angularSpeed          = 360f;
                _agent.acceleration          = 10f;
                _agent.radius                = 0.3f;
                _agent.height                = 2.0f;
                _agent.baseOffset            = 0f; // Assicurati che il pivot sia ai piedi
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                _agent.avoidancePriority     = Random.Range(30, 70);
            }

            var col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0, 1.0f, 0);
                col.radius = 0.3f;
                col.height = 2.0f;
                col.isTrigger = false; // Deve essere solido ma gestito dal NavMesh
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
            _targetDepotCtrl = FindNearestDepotController();
            _arrivedAtNode  = false;
            _chopTimer      = 0f;
            CurrentState    = VillagerState.ChoppingWood;
        }

        private void UpdateChoppingWood()
        {
            if (_targetResource == null) { GoIdle(); return; }

            // Smetti se il deposito è pieno (WoodDepot è il deposito unico attuale)
            var depot = WoodDepot.Instance;
            if (depot != null && depot.WoodCount >= depot.MaxWood)
            {
                _targetResource.Release();
                _targetResource  = null;
                _targetDepotCtrl = null;
                GoIdleDirect();
                return;
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
                    // Preleva solo quanto serve a riempire il deposito
                    int spaceLeft = (depot != null) ? (depot.MaxWood - depot.WoodCount) : 1;
                    _carriedWoodAmount = _targetResource.TakeResource(spaceLeft);
                    
                    GoCarryingWood();
                    return;
                }

                if (IsExhausted)
                {
                    if (_targetResource != null) _targetResource.Release();
                    _targetResource  = null;
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
            if (_targetDepotCtrl == null)
            {
                // Nessun depot fisico: consegna istantanea
                WoodDepot.Instance?.DepositWood(_carriedWoodAmount);
                _carriedWoodAmount = 0;
                _targetResource  = null;
                _targetDepotCtrl = null;
                if (IsExhausted) StartAutoSleep(); else GoIdleDirect();
                return;
            }

            Vector3 depotPos = Flat(_targetDepotCtrl.GetDeliveryPosition());
            MoveTo(depotPos);

            if (AgentReachedTarget(depotPos, taskStopDistance))
            {
                WoodDepot.Instance?.DepositWood(_carriedWoodAmount);
                _carriedWoodAmount = 0;
                _targetResource  = null;
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
            if (_targetResource != null) _targetResource.Release();
            _targetResource  = null;
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

            // RESET IMMEDIATO ANIMAZIONI: fermiamo chopping o altro prima di partire
            SetAnim(false, false);

            if (_targetResource != null) _targetResource.Release();
            _targetResource      = null;
            _targetDepotCtrl = null;

            _targetHouse   = house;
            
            // FASE 1: Navigazione verso la soglia (esterna) della porta
            _doorThreshold   = house.GetDoorThreshold();
            _sleepTarget     = house.GetSleepPosition();
            _isEnteringHouse = false;

            // Assicuriamoci che l'agente sia attivo per la prima fase
            if (_agent != null) 
            {
                _agent.enabled = true;
                _agent.radius  = 0.3f; // Raggio normale per navigazione esterna
            }

            _sleepDuration = house.sleepDuration;
            _sleepTimer    = 0f;
            _onWakeUp      = null;
            CurrentState   = VillagerState.GoingToSleep;
            SetAnim(true, false);

            _targetHouse?.OpenDoor();
        }

        private void UpdateGoingToSleep()
        {
            if (!_isEnteringHouse)
            {
                // FASE 1: Avvicinamento alla porta col NavMesh
                MoveTo(_doorThreshold);

                // Quando siamo vicini alla soglia (1.2 unità) passiamo alla fase 2
                float distToDoor = Vector3.Distance(Flat(transform.position), Flat(_doorThreshold));
                if (distToDoor <= 1.2f)
                {
                    _isEnteringHouse = true;
                    // FASE 2: Entrata fisica. Disabilitiamo l'agente per ignorare il NavMesh della casa
                    if (_agent != null) _agent.enabled = false;
                    Debug.Log("[VillagerController] Arrivato alla soglia, procedo all'entrata fisica.");
                }
            }
            else
            {
                // FASE 2: Movimento rettilineo verso l'interno (agente disabilitato)
                // Usiamo una velocità leggermente superiore per l'entrata per evitare incertezze
                transform.position = Vector3.MoveTowards(transform.position, _sleepTarget, moveSpeed * 1.2f * Time.deltaTime);
                
                // Rotazione verso il target
                Vector3 dir = (_sleepTarget - transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

                float distToSleep = Vector3.Distance(transform.position, _sleepTarget);
                if (distToSleep <= 0.2f)
                {
                    CurrentState = VillagerState.Sleeping;
                    _sleepTimer  = 0f;
                    SetAnim(false, false);

                    // Forza posizione finale esatta
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
                Debug.Log($"[VillagerController] Sveglio. Energia: {Energy:0}");

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
            
            // Riabilita l'agente per uscire
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

            // Ripristina il raggio standard
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

            // Trigger StartWalking only if we transition from not walking to walking
            if (walking && !_animator.GetBool(ParamWalking))
            {
                _animator.SetTrigger(ParamStartWalk);
            }

            _animator.SetBool(ParamWalking,  walking);
            _animator.SetBool(ParamChopping, chopping);
        }

        // ── Utilita' ───────────────────────────────────────────────────

        private void MoveTo(Vector3 target)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                // Evita di resettare pathPending ogni frame: ricalcola solo se destinazione cambiata
                if (!_agent.hasPath || _agent.pathPending ||
                    Vector3.Distance(_agent.destination, target) > 0.05f)
                {
                    _agent.SetDestination(target);
                }
                return;
            }
            // Fallback senza NavMesh
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime);
            Vector3 dir = target - transform.position;
            dir.y = 0f;
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
            SetVisibility(true); // Ensure visible if it was hidden
            GoIdleDirect();
            Debug.Log($"[VillagerController] {name} REVIVED.");
        }

        public void ForceIdle()
        {
            if (_targetResource  != null) _targetResource.Release();
            if (_targetBench != null) _targetBench.Vacate();
            _targetResource  = null;
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
                VillagerState.Dead         => "Dead",
                _                          => ""
            };
        }
    }
}
