using UnityEngine;

namespace DivinePrototype
{
    public enum DivinePower
    {
        None,
        SpawnMale,
        SpawnFemale,
        SpawnDog,
        SpawnCat,
        Smite,   // colpisce e danneggia oggetti/villager
        Repair,  // ripristina oggetti danneggiati
        Revive   // riporta in vita un villager morto
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
        public float faithCostVillager = 20f;

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
            }
        }

        private void OnVillagerTapped(VillagerController villager)
        {
            switch (PendingPower)
            {
                case DivinePower.Smite:
                    LightningStrike.Spawn(villager.transform.position + Vector3.up * 0.5f);
                    if (faithSystem != null) faithSystem.AddFaith(-15f);
                    
                    // Trigger death
                    villager.Die();
                    
                    StartCoroutine(FlashVillager(villager, new Color(1f, 0.9f, 0.1f), 0.3f));
                    Debug.Log($"[DivineActionSystem] Smite villager: {villager.name}");
                    break;

                case DivinePower.Revive:
                    if (villager.CurrentState == VillagerController.VillagerState.Dead)
                    {
                        if (faithSystem != null) faithSystem.AddFaith(-15f);
                        villager.Revive(0.5f);
                        StartCoroutine(FlashVillager(villager, new Color(0.8f, 1f, 0.8f), 1.0f));
                        Debug.Log($"[DivineActionSystem] Revive villager: {villager.name}");
                        ClearPendingPower();
                    }
                    break;
            }
        }

        private void OnObjectTapped(DamageableObject obj)
        {
            switch (PendingPower)
            {
                case DivinePower.Smite:
                    LightningStrike.Spawn(obj.transform.position + Vector3.up * 0.5f);
                    obj.TakeDamage();
                    if (faithSystem != null) faithSystem.AddFaith(-5f);
                    Debug.Log($"[DivineActionSystem] Smite su {obj.name} → stato: {obj.CurrentState}");
                    
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
                    Debug.Log($"[DivineActionSystem] Riparato {obj.name}");
                    
                    DivineEventManager.Broadcast(new DivineEvent { 
                        Type = DivineEventType.Repair, 
                        Position = obj.transform.position, 
                        Target = obj.gameObject, 
                        Radius = 15f 
                    });
                    break;

                default:
                    // Nessun potere attivo: tap neutro, mostra solo feedback
                    Debug.Log($"[DivineActionSystem] Tap su oggetto: {obj.name} (stato: {obj.CurrentState})");
                    break;
            }
        }

        /// <summary>
        /// Chiamato da AxeActionUI quando l'icona ascia viene trascinata sul popolano.
        /// </summary>
        public void GiveAxe(VillagerController villager)
        {
            if (gameState == null || gameState.HasAxe) return;
            gameState.HasAxe = true;
            villager.HasPersonalAxe = true;
            StartCoroutine(FlashVillager(villager, new Color(1f, 0.85f, 0.2f), 1.2f));
            Debug.Log("[DivineActionSystem] Ascia donata al popolano.");
        }

        /// <summary>
        /// Chiamato da PickaxeActionUI quando l'icona piccone viene trascinata sul popolano.
        /// </summary>
        public void GivePickaxe(VillagerController villager)
        {
            // Per ora non usiamo HasPickaxe in GameStateSystem per non corromperlo,
            // ma potremmo aggiungerlo in futuro se necessario.
            villager.HasPersonalPickaxe = true;
            StartCoroutine(FlashVillager(villager, new Color(0.2f, 0.85f, 1f), 1.2f));
            Debug.Log("[DivineActionSystem] Piccone donato al popolano.");
        }

        // ── Spawn ────────────────────────────────────────────────────────

        /// <summary>
        /// Chiamato da VillagerActionUI dopo un drag-drop valido.
        /// </summary>
        public void SpawnVillagerAt(bool isFemale, Vector3 worldPos)
            => TrySpawnVillager(isFemale, worldPos);

        private void TrySpawnVillager(bool isFemale, Vector3 tapPosition)
        {
            var prefab = isFemale ? villagerFemalePrefab : villagerMalePrefab;
            if (prefab == null)
            {
                Debug.LogError($"[DivineActionSystem] Prefab {(isFemale ? "femmina" : "maschio")} non assegnato.");
                return;
            }

            GameObject go = Instantiate(prefab, tapPosition, Quaternion.identity);
            go.name = isFemale ? "Villager_F" : "Villager_M";

            if (gameState != null) gameState.HasVillager = true;

            ClearPendingPower();
            Debug.Log($"[DivineActionSystem] Villager creato ({(isFemale ? "donna" : "uomo")}).");
        }

        private System.Collections.IEnumerator FlashVillager(VillagerController villager, Color flashColor, float duration)
        {
            // Supporta sia SkinnedMeshRenderer (vecchia mesh) che MeshRenderer (nuove mesh)
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
         if (mat != null) mat.color = original;
        }


    }
}
