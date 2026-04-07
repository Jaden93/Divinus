using System.Collections;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Componente sulla casa costruita.
    /// Gestisce capacità sonno e animazione apertura/chiusura porta.
    /// </summary>
    public class HouseController : MonoBehaviour
    {
        [Header("Sleep")]
        public float sleepDuration = 8f;
        public int   maxOccupants  = 1;

        [Header("Porta")]
        [Tooltip("Gradi di apertura porta attorno all'asse Y locale")]
        public float doorOpenAngle  = 90f;
        [Tooltip("Secondi per aprire/chiudere la porta")]
        public float doorAnimSpeed  = 0.4f;
        [Tooltip("Offset dell'asse cerniera rispetto al pivot della porta (locale porta)")]
        public Vector3 hingeOffset  = new Vector3(-0.45f, 0f, 0f);

        public bool IsFull => _occupants >= maxOccupants;

        private Transform _sleepPoint;
        private Transform _doorMesh;       // il mesh della porta
        private Transform _doorPivot;      // pivot cerniera (creato a runtime)
        private bool      _doorOpen;
        private int       _occupants = 0;

        private void Start()
        {
            _sleepPoint = transform.Find("SleepPoint");
            if (_sleepPoint == null) _sleepPoint = transform.Find("Door");

            // Cerca il mesh della porta tra i figli
            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.name.Contains("door") || mr.name.Contains("Door"))
                {
                    _doorMesh = mr.transform;
                    break;
                }
            }

            if (_doorMesh != null)
                SetupDoorPivot();

            // Sostituisce il BoxCollider solido con lastre di parete
            // così il NavMeshAgent può fisicamente attraversare il portale
            ReplaceColliderWithWallSlabs();

            Debug.Log($"[HouseController] Casa pronta. Porta: {(_doorMesh != null ? _doorMesh.name : "non trovata")}");
        }

        // ── Occupancy ─────────────────────────────────────────────────────

        public bool TryOccupy()
        {
            if (_occupants >= maxOccupants) return false;
            _occupants++;
            return true;
        }

        public void Vacate()
        {
            _occupants = Mathf.Max(0, _occupants - 1);
        }

        public Vector3 GetSleepPosition()
        {
            // Usa il centro dei bounds del mesh della porta (i vertici sono baked,
            // quindi il transform.position non riflette la posizione visiva reale)
            if (_doorMesh != null)
            {
                var mf = _doorMesh.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 doorWorldCenter = _doorMesh.TransformPoint(mf.sharedMesh.bounds.center);
                    Vector3 towardDoor = doorWorldCenter - transform.position;
                    towardDoor.y = 0f;
                    if (towardDoor.sqrMagnitude > 0.001f)
                    {
                        // Punto sulla soglia della porta (lievemente fuori), garantito sul NavMesh
                        Vector3 threshold = doorWorldCenter + towardDoor.normalized * 0.5f;
                        return new Vector3(threshold.x, 0f, threshold.z);
                    }
                }
            }
            // Ultimo fallback: davanti la porta (-forward per la maggior parte dei modelli)
            Vector3 front = transform.position - transform.forward * 1.5f;
            return new Vector3(front.x, 0f, front.z);
        }

        // ── Porta ─────────────────────────────────────────────────────────

        /// <summary>Apri la porta (chiamato quando il villager arriva).</summary>
        public void OpenDoor()
        {
            if (_doorPivot == null || _doorOpen) return;
            _doorOpen = true;
            StopAllCoroutines();
            StartCoroutine(AnimateDoor(doorOpenAngle));
        }

        /// <summary>Chiudi la porta (chiamato al risveglio).</summary>
        public void CloseDoor()
        {
            if (_doorPivot == null || !_doorOpen) return;
            _doorOpen = false;
            StopAllCoroutines();
            StartCoroutine(AnimateDoor(0f));
        }

        // ── Collider pareti ───────────────────────────────────────────────────

        /// <summary>
        /// Rimuove il BoxCollider solido e lo sostituisce con 6 lastre sottili
        /// (pareti fisiche) lasciando libera l'apertura della porta.
        /// Dimensioni basate sul modello med_house_small_a.
        /// </summary>
        private void ReplaceColliderWithWallSlabs()
        {
            // Rimuovi il BoxCollider solido esistente
            var existing = GetComponent<BoxCollider>();
            if (existing != null) Destroy(existing);

            // Dimensioni casa (da mesh bounds)
            const float hw    = 2f;    // half-width  X
            const float hd    = 1.5f;  // half-depth  Z
            const float houseH = 5.4f;
            const float thick  = 0.2f;

            // Porta: larghezza 0.9m centrata, altezza 2m, sulla parete frontale Z=-hd
            const float doorHW = 0.45f;  // half door width
            const float doorH  = 2f;

            float wallMidY   = houseH / 2f;
            float aboveDoorH = houseH - doorH;
            float aboveDoorY = doorH + aboveDoorH / 2f;

            // Parete frontale sinistra  [X: -hw … -doorHW]
            AddWallSlab(new Vector3(-(hw + doorHW) / 2f, wallMidY, -hd),
                        new Vector3(hw - doorHW, houseH, thick));

            // Parete frontale destra   [X:  doorHW … hw]
            AddWallSlab(new Vector3( (hw + doorHW) / 2f, wallMidY, -hd),
                        new Vector3(hw - doorHW, houseH, thick));

            // Parete frontale sopra la porta
            AddWallSlab(new Vector3(0f, aboveDoorY, -hd),
                        new Vector3(doorHW * 2f, aboveDoorH, thick));

            // Parete posteriore
            AddWallSlab(new Vector3(0f, wallMidY, hd),
                        new Vector3(hw * 2f, houseH, thick));

            // Parete laterale sinistra
            AddWallSlab(new Vector3(-hw, wallMidY, 0f),
                        new Vector3(thick, houseH, hd * 2f));

            // Parete laterale destra
            AddWallSlab(new Vector3( hw, wallMidY, 0f),
                        new Vector3(thick, houseH, hd * 2f));
        }

        private void AddWallSlab(Vector3 center, Vector3 size)
        {
            var go = new GameObject("WallSlab") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = center;
            bc.size   = size;

            // Scolpisce il NavMesh a runtime: le pareti diventano non-walkable
            // lasciando libera solo l'apertura della porta
            var obs = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
            obs.center  = Vector3.zero;   // locale al GO, già posizionato dal parent
            obs.size    = size;
            obs.carving = true;
        }

        private void SetupDoorPivot()
        {
            // Crea un pivot alla cerniera della porta (bordo sinistro)
            _doorPivot = new GameObject("DoorPivot").transform;
            _doorPivot.SetParent(transform, false);

            // Posiziona il pivot dove si trova la cerniera nel world
            _doorPivot.position = _doorMesh.TransformPoint(hingeOffset);
            _doorPivot.rotation = _doorMesh.rotation;

            // Re-parent il mesh al pivot mantenendo la posizione world
            _doorMesh.SetParent(_doorPivot, true);
        }

        private IEnumerator AnimateDoor(float targetAngle)
        {
            float startAngle = _doorPivot.localEulerAngles.y;
            // Normalizza da 0-360 al range atteso
            if (startAngle > 180f) startAngle -= 360f;

            float elapsed = 0f;
            while (elapsed < doorAnimSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / doorAnimSpeed);
                float angle = Mathf.Lerp(startAngle, targetAngle, t);
                _doorPivot.localEulerAngles = new Vector3(0f, angle, 0f);
                yield return null;
            }
            _doorPivot.localEulerAngles = new Vector3(0f, targetAngle, 0f);
        }
    }
}
