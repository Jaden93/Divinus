using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Gestisce la costruzione della casa.
    /// Viene avviato da HouseActionUI con StartConstruction(position).
    /// Spawna le fondamenta al punto indicato, mostra un timer di 10 s,
    /// poi rimuove fondamenta e timer, e spawna House.prefab.
    /// </summary>
    public class ConstructionSite : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject housePrefab;
        public GameObject foundationsPrefab;
        public GameStateSystem gameState;
        public float buildDuration = 10f;

        [Header("Fondamenta")]
        [Tooltip("Scala XZ delle fondamenta spawned. Regola per farla coincidere con l'impronta visiva della casa.")]
        public Vector2 foundationsSize = new Vector2(5f, 5f);

        [Header("Costo")]
        public int woodCost = 6;

        [Header("Timer UI")]
        public Canvas uiCanvas;   // Assegna il Canvas radice della scena

        // Riferimenti runtime per pulizia garantita
        private GameObject _timerGO;
        private GameObject _foundations;
        private bool       _building = false;
        public  bool       IsBuilding => _building;

        private void Start()
        {
            if (gameState == null) gameState = FindObjectOfType<GameStateSystem>();
            if (uiCanvas  == null) uiCanvas  = FindObjectOfType<Canvas>();
        }

        private void OnDisable()
        {
            // Sicurezza: pulisci se il componente viene disabilitato
            CleanupRuntime();
        }

        /// <summary>Chiamato da HouseActionUI dopo il drop sul terreno.</summary>
        public void StartConstruction(Vector3 worldPosition)
        {
            if (_building) return;
            _building = true;

            // Consuma la legna
            var depot = FindObjectOfType<WoodDepot>();
            depot?.ConsumeWood(woodCost);

            Debug.Log("[ConstructionSite] Costruzione avviata a " + worldPosition);
            StartCoroutine(BuildRoutine(worldPosition));
        }

        private IEnumerator BuildRoutine(Vector3 pos)
        {
            // 1. Spawna fondamenta
            if (foundationsPrefab != null)
            {
                _foundations = Instantiate(foundationsPrefab, pos, Quaternion.identity);
                _foundations.transform.localScale = new Vector3(foundationsSize.x, 0.25f, foundationsSize.y);
            }

            // Feedback visivo: costruzione avviata
            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("Construction started!", pos, new Color(0.3f, 0.8f, 1f));

            // 2. Crea label timer
            _timerGO = CreateTimerLabel();

            // 3. Countdown
            var cam = Camera.main;
            float elapsed = 0f;
            while (elapsed < buildDuration)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Ceil(buildDuration - elapsed);

                if (_timerGO != null)
                {
                    // Aggiorna testo
                    var txt = _timerGO.GetComponentInChildren<Text>();
                    if (txt != null)
                        txt.text = Mathf.Max(0, remaining).ToString("0") + "s";

                    // Segui la posizione world dell'edificio
                    if (cam != null)
                        _timerGO.transform.position = cam.WorldToScreenPoint(pos + Vector3.up * 2.5f);
                }

                yield return null;
            }

            // 4. Pulisci
            CleanupRuntime();

            // 5. Spawna casa
            SpawnHouse(pos);
        }

        private void CleanupRuntime()
        {
            if (_timerGO      != null) { Destroy(_timerGO);      _timerGO      = null; }
            if (_foundations  != null) { Destroy(_foundations);   _foundations  = null; }
        }

        private void SpawnHouse(Vector3 pos)
        {
            if (housePrefab == null)
            {
                Debug.LogWarning("[ConstructionSite] housePrefab non assegnato.");
                _building = false;
                return;
            }

            pos.y = 0f;

            // Ruota la porta verso la camera
            Quaternion houseRot = Quaternion.identity;
            if (Camera.main != null)
            {
                Vector3 camFwd = Camera.main.transform.forward;
                camFwd.y = 0f;
                if (camFwd.sqrMagnitude > 0.001f)
                    houseRot = Quaternion.LookRotation(camFwd.normalized);
            }
            var house = Instantiate(housePrefab, pos, houseRot);
            house.SetActive(true);

            // BoxCollider: nuovo modello med_house_small_a ha pivot centrato (no offset X)
            var bc = house.GetComponent<BoxCollider>();
            if (bc == null) bc = house.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 2.7f, 0f);
            bc.size   = new Vector3(4f, 5.4f, 3f);

            if (house.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obs = house.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.carving = true;
                obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
                obs.size    = new Vector3(2.5f, 2f, 2.5f);
                obs.center  = new Vector3(0f, 1f, 0f);
            }

            if (gameState != null) gameState.HasHouse = true;

            Debug.Log("[ConstructionSite] Casa costruita!");

            // Feedback visivo: costruzione completata
            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.Spawn("House complete!", pos, new Color(0.4f, 1f, 0.4f));

            _building = false;
        }

        // ── UI Timer ──────────────────────────────────────────────────────

        private GameObject CreateTimerLabel()
        {
            if (uiCanvas == null)
            {
                Debug.LogWarning("[ConstructionSite] uiCanvas null, timer UI non creato.");
                return null;
            }

            // Container con sfondo
            var go = new GameObject("ConstructionTimer");
            go.transform.SetParent(uiCanvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            rt.sizeDelta        = new Vector2(220f, 80f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            // Testo
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);

            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            textGO.AddComponent<CanvasRenderer>();
            var txt       = textGO.AddComponent<Text>();
            txt.text      = "Costruzione\n10s";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 22;
            txt.color     = Color.white;

            // Font: prova nomi comuni per diverse versioni Unity
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) txt.font = f;

            return go;
        }
    }
}
