using UnityEngine;
using System.Collections;

namespace DivinePrototype
{
    public enum DivinePower
    {
        None,
        SpawnMale,
        SpawnFemale,
        SpawnDog,
        SpawnCat,
        Smite,   // colpisce e danneggia oggetti/villager/risorse
        Repair,  // ripristina oggetti danneggiati
        Revive,  // riporta in vita un villager morto
        Messenger // incarica un villager di diffondere la parola
    }

    /// <summary>
    /// Riceve gli input di TouchInputSystem e li traduce in atti divini
    /// in base al potere selezionato nel menu.
    /// </summary>
    public class DivineActionSystem : MonoBehaviour
    {
        [Header("Riferimenti scena")]
        public GameStateSystem gameState;
        public TouchInputSystem touchInput;
        public VillageFaithSystem faithSystem;

        [Header("Spawn villager")]
        public GameObject villagerMalePrefab;
        public GameObject villagerFemalePrefab;
        public Transform spawnPoint;

        [Header("Costi fede")]
        public float faithCostVillager = 0f;
        public float faithCostRevive   = 0f;
        public float faithCostSmite    = 0f;
        public float faithCostMessenger = 0f;


        public GameObject smiteVFXPrefab;
        public DivinePower PendingPower { get; private set; } = DivinePower.None;

        private void Start()
        {
            if (touchInput == null)
                touchInput = FindObjectOfType<TouchInputSystem>();
            if (faithSystem == null)
                faithSystem = FindObjectOfType<VillageFaithSystem>();

            if (touchInput != null)
            {
                touchInput.onTerrainTapped.AddListener(OnTerrainTapped);
                touchInput.onVillagerTapped.AddListener(OnVillagerTapped);
                touchInput.onObjectTapped.AddListener(OnObjectTapped);
                touchInput.onResourceTapped.AddListener(OnResourceTapped);
                touchInput.onTombTapped.AddListener(OnTombTapped);
            }
            else
            {
                Debug.LogWarning("[DivineActionSystem] TouchInputSystem non trovato.");
            }
        }

        private void OnDestroy()
        {
            if (touchInput != null)
            {
                touchInput.onTerrainTapped.RemoveListener(OnTerrainTapped);
                touchInput.onVillagerTapped.RemoveListener(OnVillagerTapped);
                touchInput.onObjectTapped.RemoveListener(OnObjectTapped);
                touchInput.onResourceTapped.RemoveListener(OnResourceTapped);
            }
        }

        public void SetPendingPower(DivinePower power)
        {
            PendingPower = power;
            Debug.Log($"[DivineActionSystem] Potere selezionato: {power}");
        }

        public void ClearPendingPower()
        {
            PendingPower = DivinePower.None;
        }

        // ── Atti divini ──────────────────────────────────────────────────

        private void OnTerrainTapped(Vector3 worldPos)
        {
            switch (PendingPower)
            {
                case DivinePower.SpawnMale:
                    TrySpawnVillager(false, worldPos);
                    break;
                case DivinePower.SpawnFemale:
                    TrySpawnVillager(true, worldPos);
                    break;
                case DivinePower.Smite:
                    LightningStrike.Spawn(worldPos, smiteVFXPrefab);
                    if (faithSystem != null) faithSystem.AddFaith(-faithCostSmite);
                    
                    DivineEventManager.Broadcast(new DivineEvent { 
                        Type = DivineEventType.Smite, 
                        Position = worldPos, 
                        Target = null, 
                        Radius = 15f 
                    });
                    break;
            }
        }

        private void OnVillagerTapped(VillagerController villager)
        {
            switch (PendingPower)
            {
                case DivinePower.Smite:
                    LightningStrike.Spawn(villager.transform.position + Vector3.up * 0.5f, smiteVFXPrefab);
                    if (faithSystem != null) faithSystem.AddFaith(-faithCostSmite);
                    
                    // Morte istantanea
                    villager.Die();
                    
                    StartCoroutine(FlashVillager(villager, new Color(1f, 0.9f, 0.1f), 0.3f));
                    
                    if (FloatingTextSpawner.Instance != null)
                        FloatingTextSpawner.Instance.Spawn("Smote!", villager.transform.position + Vector3.up * 2f, Color.red);

                    DivineEventManager.Broadcast(new DivineEvent { 
                        Type = DivineEventType.Smite, 
                        Position = villager.transform.position, 
                        Target = villager.gameObject, 
                        Radius = 20f 
                    });
                    break;

                case DivinePower.Revive:
                    if (villager.CurrentState == VillagerController.VillagerState.Dead)
                    {
                        if (faithSystem != null) faithSystem.AddFaith(-faithCostRevive);
                        
                        villager.Revive(0.5f);
                        ReviveVFX.Spawn(villager.transform.position); // Spawna l'effetto procedurale
                        StartCoroutine(FlashVillager(villager, new Color(0.4f, 1f, 0.4f), 1.0f));
                        
                        DivineEventManager.Broadcast(new DivineEvent { 
                            Type = DivineEventType.Revive, 
                            Position = villager.transform.position, 
                            Target = villager.gameObject, 
                            Radius = 15f 
                        });
                        
                        ClearPendingPower();
                    }
                    break;

                case DivinePower.Messenger:
                    if (villager.CurrentState != VillagerController.VillagerState.Dead)
                    {
                        if (faithSystem != null) faithSystem.AddFaith(-faithCostMessenger);

                        StartCoroutine(MessengerRoutine(villager));
                        StartCoroutine(FlashVillager(villager, new Color(1f, 1f, 0.4f), 1.0f));
                        ClearPendingPower();
                    }
                    break;
            }
        }

        private IEnumerator MessengerRoutine(VillagerController messenger)
        {
            messenger.PauseWork();
            messenger.SetSocialState(VillagerController.VillagerState.Messenger);

            // Trova un bersaglio lontano
            VillagerController target = null;
            VillagerController[] all = FindObjectsOfType<VillagerController>();
            float maxDist = 0f;

            foreach (var v in all)
            {
                if (v == messenger || v.CurrentState == VillagerController.VillagerState.Dead) continue;
                float d = Vector3.Distance(messenger.transform.position, v.transform.position);
                if (d > maxDist)
                {
                    maxDist = d;
                    target = v;
                }
            }

            if (target != null)
            {
                if (FloatingTextSpawner.Instance != null)
                    FloatingTextSpawner.Instance.Spawn("📣 GO FORTH!", messenger.transform.position + Vector3.up * 2.5f, Color.yellow);

                float timeout = 10f;
                while (Vector3.Distance(messenger.transform.position, target.transform.position) > 2.0f && timeout > 0)
                {
                    if (messenger.CurrentState != VillagerController.VillagerState.Messenger) yield break;
                    messenger.MoveToSocialTarget(target.transform.position, 2.0f);
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (timeout > 0 && messenger.CurrentState == VillagerController.VillagerState.Messenger)
                {
                    // Messaggio consegnato
                    DivineEventManager.Broadcast(new DivineEvent {
                        Type = DivineEventType.Messenger,
                        Position = messenger.transform.position,
                        Target = messenger.gameObject,
                        Radius = 10f
                    });

                    target.ModifyLoyalty(10f);
                    if (FloatingTextSpawner.Instance != null)
                        FloatingTextSpawner.Instance.Spawn("🙏 DIVINE WORD", target.transform.position + Vector3.up * 2.5f, Color.cyan);
                    
                    yield return new WaitForSeconds(2f);
                }
            }

            messenger.ResumeWork();
        }

        private void OnObjectTapped(DamageableObject obj)
        {
            switch (PendingPower)
            {
                case DivinePower.Smite:
                    LightningStrike.Spawn(obj.transform.position + Vector3.up * 0.5f, smiteVFXPrefab);
                    obj.TakeDamage();
                    if (faithSystem != null) faithSystem.AddFaith(-5f);
                    
                    DivineEventManager.Broadcast(new DivineEvent { 
                        Type = DivineEventType.Smite, 
                        Position = obj.transform.position, 
                        Target = obj.gameObject, 
                        Radius = 15f 
                    });
                    break;

                case DivinePower.Repair:
                    obj.Repair();
                    if (faithSystem != null) faithSystem.AddFaith(3f);
                    
                    DivineEventManager.Broadcast(new DivineEvent { 
                        Type = DivineEventType.Repair, 
                        Position = obj.transform.position, 
                        Target = obj.gameObject, 
                        Radius = 15f 
                    });
                    break;
            }
        }

        private void OnResourceTapped(ResourceNode node)
        {
            if (PendingPower != DivinePower.Smite) return;

            // Usa la logica interna del nodo per il depletamento da Smite (che spawna i cubi)
            node.SmiteDeplete();

            DivineEventManager.Broadcast(new DivineEvent { 
                Type = DivineEventType.Smite, 
                Position = node.transform.position, 
                Target = node.gameObject, 
                Radius = 15f 
            });
        }

        private void OnTombTapped(TombController tomb)
        {
            if (PendingPower == DivinePower.Revive)
            {
                if (faithSystem != null) faithSystem.AddFaith(-faithCostRevive);

                tomb.ReviveBuriedVillager();
                ClearPendingPower();
            }
        }

        public void GiveAxe(VillagerController villager)
        {
            if (gameState == null || gameState.HasAxe) return;
            gameState.HasAxe = true;
            villager.HasPersonalAxe = true;
            StartCoroutine(FlashVillager(villager, new Color(1f, 0.85f, 0.2f), 1.2f));
        }

        public void GivePickaxe(VillagerController villager)
        {
            villager.HasPersonalPickaxe = true;
            StartCoroutine(FlashVillager(villager, new Color(0.2f, 0.85f, 1f), 1.2f));
        }

        public void SpawnVillagerAt(bool isFemale, Vector3 worldPos)
            => TrySpawnVillager(isFemale, worldPos);

        private void TrySpawnVillager(bool isFemale, Vector3 tapPosition)
        {
            var prefab = isFemale ? villagerFemalePrefab : villagerMalePrefab;
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, tapPosition, Quaternion.identity);
            go.name = isFemale ? "Villager_F" : "Villager_M";
            if (gameState != null) gameState.HasVillager = true;
            ClearPendingPower();
        }

        private System.Collections.IEnumerator FlashVillager(VillagerController villager, Color flashColor, float duration)
        {
            Renderer rend = villager.GetComponentInChildren<SkinnedMeshRenderer>();
            if (rend == null) rend = villager.GetComponentInChildren<MeshRenderer>();
            if (rend == null) yield break;
            var mat = rend.material;
            Color original = mat.color;
            mat.color = flashColor;
            yield return new WaitForSeconds(duration);
            if (mat != null) mat.color = original;
        }
    }
}
