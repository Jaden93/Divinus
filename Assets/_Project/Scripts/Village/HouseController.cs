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
            // Cerca SleepPoint (case-insensitive)
            foreach (Transform child in transform)
            {
                if (child.name.Equals("SleepPoint", System.StringComparison.OrdinalIgnoreCase))
                { _sleepPoint = child; break; }
            }

            // Cerca il mesh della porta tra i figli (case-insensitive)
            foreach (var mr in GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.name.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _doorMesh = mr.transform;
                    break;
                }
            }

            if (_doorMesh != null)
                SetupDoorPivot();

            // Blocca il passaggio attraverso l'expansion (se presente)
            SetupExpansionCollider();

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

        public Vector3 GetDoorThreshold()
        {
            if (_doorMesh != null)
            {
                // Usiamo la rotazione della casa per determinare il "davanti"
                // Assumiamo che la porta sia sul lato frontale (Z+) o che il mesh della porta sia indicativo
                Vector3 towardOutside = -transform.forward; // Di solito le case sono orientate con forward verso l'interno o l'esterno
                
                // Proviamo a essere più precisi: usiamo il vettore centro casa -> porta
                Vector3 doorPos = _doorMesh.position;
                towardOutside = (doorPos - transform.position).normalized;
                towardOutside.y = 0f;

                Vector3 outside = doorPos + towardOutside * 1.5f;
                outside.y = transform.position.y;
                return outside;
            }
            return transform.position - transform.forward * 3f;
        }

        public Vector3 GetSleepPosition()
        {
            if (_sleepPoint != null) return _sleepPoint.position;

            if (_doorMesh != null)
            {
                Vector3 doorPos = _doorMesh.position;
                Vector3 towardInside = (transform.position - doorPos).normalized;
                towardInside.y = 0f;
                
                Vector3 inside = doorPos + towardInside * 2.0f; 
                inside.y = transform.position.y; 
                return inside;
            }

            return transform.position;
        }

        // ── Porta ─────────────────────────────────────────────────────────

        /// <summary>Apri la porta (chiamato quando il villager arriva).</summary>
        public void OpenDoor()
        {
            Debug.Log($"[HouseController] OpenDoor chiamato. _doorPivot={(_doorPivot != null ? _doorPivot.name : "NULL")} _doorOpen={_doorOpen}");
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

        // ── Expansion ─────────────────────────────────────────────────────────

        /// <summary>
        /// Aggiunge collider e NavMeshObstacle all'expansion della casa
        /// in modo che il villager non possa attraversarla.
        /// </summary>
        private void SetupExpansionCollider()
        {
            foreach (Transform child in transform)
            {
                if (!child.name.Equals("expansion", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip se ha già un collider
                if (child.GetComponent<Collider>() != null) return;

                var renderer = child.GetComponent<Renderer>();
                if (renderer == null) return;

                Bounds b = renderer.bounds;

                // BoxCollider basato sui bounds del renderer
                var box = child.gameObject.AddComponent<BoxCollider>();
                // Converti bounds world in local space del child
                box.center = child.InverseTransformPoint(b.center);
                Vector3 localSize = new Vector3(
                    b.size.x / child.lossyScale.x,
                    b.size.y / child.lossyScale.y,
                    b.size.z / child.lossyScale.z);
                box.size = localSize;

                // NavMeshObstacle per bloccare il pathfinding
                var obs = child.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
                obs.center  = box.center;
                obs.size    = box.size;
                obs.carving = true;

                Debug.Log($"[HouseController] Expansion collider aggiunto: bounds={b.center} size={b.size}");
                return;
            }
        }

        // ── Collider pareti ───────────────────────────────────────────────────

        /// <summary>
        /// Calcola le pareti dai bounds reali di "exterior" e "door",
        /// lasciando libero solo il varco della porta.
        /// </summary>
        private void ReplaceColliderWithWallSlabs()
        {
            var existing = GetComponent<BoxCollider>();
            if (existing != null) Destroy(existing);

            // Trova i bounds world di exterior e door
            Renderer extR = null, doorR = null;
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.name.Equals("exterior", System.StringComparison.OrdinalIgnoreCase))
                    extR = mr;
                if (_doorMesh != null && mr.transform == _doorMesh)
                    doorR = mr;
            }
            if (extR == null) return;

            Bounds ext = extR.bounds;
            const float thick = 0.2f;
            float houseH = ext.size.y;
            float midY   = ext.center.y;

            if (doorR != null)
            {
                Bounds door = doorR.bounds;

                // Determina su quale faccia si trova la porta
                float distToMinZ = Mathf.Abs(door.center.z - ext.min.z);
                float distToMaxZ = Mathf.Abs(door.center.z - ext.max.z);
                float distToMinX = Mathf.Abs(door.center.x - ext.min.x);
                float distToMaxX = Mathf.Abs(door.center.x - ext.max.x);
                float minFaceDist = Mathf.Min(distToMinZ, distToMaxZ, distToMinX, distToMaxX);

                // Porta sulla faccia Z-min (la più comune)
                if (Mathf.Approximately(minFaceDist, distToMinZ) || Mathf.Approximately(minFaceDist, distToMaxZ))
                {
                    float doorFaceZ = minFaceDist == distToMinZ ? ext.min.z : ext.max.z;
                    float doorLeft  = door.min.x;
                    float doorRight = door.max.x;
                    float doorTop   = door.max.y;

                    // Parete sinistra della porta
                    AddWallSlabWorld(
                        new Vector3((ext.min.x + doorLeft) / 2f, midY, doorFaceZ),
                        new Vector3(doorLeft - ext.min.x, houseH, thick));
                    // Parete destra della porta
                    AddWallSlabWorld(
                        new Vector3((doorRight + ext.max.x) / 2f, midY, doorFaceZ),
                        new Vector3(ext.max.x - doorRight, houseH, thick));
                    // Sopra la porta
                    float aboveH = ext.max.y - doorTop;
                    if (aboveH > 0.1f)
                        AddWallSlabWorld(
                            new Vector3((doorLeft + doorRight) / 2f, doorTop + aboveH / 2f, doorFaceZ),
                            new Vector3(doorRight - doorLeft, aboveH, thick));
                    // Parete opposta
                    float oppZ = minFaceDist == distToMinZ ? ext.max.z : ext.min.z;
                    AddWallSlabWorld(
                        new Vector3(ext.center.x, midY, oppZ),
                        new Vector3(ext.size.x, houseH, thick));
                }
                else
                {
                    // Porta su faccia X (analoga ma invertita)
                    float doorFaceX = minFaceDist == distToMinX ? ext.min.x : ext.max.x;
                    float doorFront = door.min.z;
                    float doorBack  = door.max.z;
                    float doorTop   = door.max.y;

                    AddWallSlabWorld(
                        new Vector3(doorFaceX, midY, (ext.min.z + doorFront) / 2f),
                        new Vector3(thick, houseH, doorFront - ext.min.z));
                    AddWallSlabWorld(
                        new Vector3(doorFaceX, midY, (doorBack + ext.max.z) / 2f),
                        new Vector3(thick, houseH, ext.max.z - doorBack));
                    float aboveH = ext.max.y - doorTop;
                    if (aboveH > 0.1f)
                        AddWallSlabWorld(
                            new Vector3(doorFaceX, doorTop + aboveH / 2f, (doorFront + doorBack) / 2f),
                            new Vector3(thick, aboveH, doorBack - doorFront));
                    float oppX = minFaceDist == distToMinX ? ext.max.x : ext.min.x;
                    AddWallSlabWorld(
                        new Vector3(oppX, midY, ext.center.z),
                        new Vector3(thick, houseH, ext.size.z));
                }
            }

            // Pareti laterali (sempre presenti)
            // Sinistra
            AddWallSlabWorld(
                new Vector3(ext.min.x, midY, ext.center.z),
                new Vector3(thick, houseH, ext.size.z));
            // Destra
            AddWallSlabWorld(
                new Vector3(ext.max.x, midY, ext.center.z),
                new Vector3(thick, houseH, ext.size.z));

            Debug.Log($"[HouseController] Pareti create da bounds: ext={ext.center} size={ext.size}");
        }

        private void AddWallSlabWorld(Vector3 worldCenter, Vector3 worldSize)
        {
            var go = new GameObject("WallSlab") { layer = gameObject.layer };
            go.transform.SetParent(transform, true);
            go.transform.position = worldCenter;
            go.transform.rotation = Quaternion.identity;

            var bc = go.AddComponent<BoxCollider>();
            bc.center = Vector3.zero;
            bc.size   = worldSize;

            var obs = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obs.shape   = UnityEngine.AI.NavMeshObstacleShape.Box;
            obs.center  = Vector3.zero;
            obs.size    = worldSize;
            obs.carving = true;
        }

        private void SetupDoorPivot()
        {
            var renderer = _doorMesh.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning("[HouseController] Door renderer NULL, pivot non creato.");
                return;
            }

            Bounds b = renderer.bounds;
            Vector3 houseCenter = transform.position;
            houseCenter.y = b.center.y;

            // Testa i 4 bordi del bounds, prendi il più vicino al centro casa (= cerniera)
            Vector3[] edges = {
                new Vector3(b.min.x, b.center.y, b.center.z),
                new Vector3(b.max.x, b.center.y, b.center.z),
                new Vector3(b.center.x, b.center.y, b.min.z),
                new Vector3(b.center.x, b.center.y, b.max.z)
            };
            Vector3 hingePos = edges[0];
            float minDist = float.MaxValue;
            foreach (var e in edges)
            {
                float d = Vector3.Distance(e, houseCenter);
                if (d < minDist) { minDist = d; hingePos = e; }
            }
            hingePos.y = 0f;

            _doorPivot = new GameObject("DoorPivot").transform;
            _doorPivot.SetParent(transform, false);
            _doorPivot.position = hingePos;
            // Usa la rotazione locale identity così localEulerAngles.y parte da 0
            _doorPivot.localRotation = Quaternion.identity;

            _doorMesh.SetParent(_doorPivot, true);

            Debug.Log($"[HouseController] DoorPivot creato a {hingePos}, localEulerY={_doorPivot.localEulerAngles.y}, door bounds={b.center} size={b.size}");
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
