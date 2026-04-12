using System.Collections.Generic;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Griglia di piazzamento sul terreno.
    /// Il feedback rosso/verde è sul preview 3D dell'elemento.
    /// Aggiungere a un GameObject vuoto nella scena (es. "GridManager").
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Griglia")]
        public float cellSize        = 2f;
        public int   halfGridW       = 12;
        public int   halfGridH       = 12;

        [Header("Collision Detection")]
        [Tooltip("Dimensione footprint per rilevamento alberi e OverlapBox fisico")]
        public float buildingFootprint  = 4f;
        [Tooltip("Distanza minima centro-centro tra edifici (impedisce stesso slot). Le case usano il collider fisico per spacing maggiore)")]
        public float minBuildingSpacing = 2.1f;

        [Header("Rilevamento ostacoli")]
        [Tooltip("Prefisso del nome degli alberi nella scena (es. 'Tree')")]
        public string treeNamePrefix = "Tree";
        [Tooltip("Raggio intorno all'albero considerato bloccato")]
        public float  treeBlockRadius = 1.3f;

        private readonly HashSet<Vector2Int> _occupied      = new();
        private readonly List<Vector3>       _treePositions = new();
        private Material _glMat;
        private Vector3  _lastSnapPos;
        private bool     _visible;

        private void Awake()
        {
            Instance = this;
            var shader = Shader.Find("Hidden/Internal-Colored");
            _glMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
            _glMat.SetInt("_ZWrite", 0);
        }

        private void Start()
        {
            CacheTreePositions();
            CacheExistingBuildings();
        }

        /// <summary>Registra in _occupied le case e le panche già presenti in scena all'avvio.</summary>
        private void CacheExistingBuildings()
        {
            int count = 0;
            foreach (var h in FindObjectsOfType<HouseController>())
            {
                _occupied.Add(WorldToCell(h.transform.position));
                count++;
            }
            foreach (var b in FindObjectsOfType<BenchController>())
            {
                _occupied.Add(WorldToCell(b.transform.position));
                count++;
            }
            if (count > 0)
                Debug.Log($"[GridManager] Registrati {count} edifici esistenti.");
        }

        /// <summary>Trova tutti gli oggetti il cui nome inizia con treeNamePrefix e ne salva le posizioni.</summary>
        private void CacheTreePositions()
        {
            _treePositions.Clear();
            foreach (var go in FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith(treeNamePrefix, System.StringComparison.OrdinalIgnoreCase))
                    _treePositions.Add(go.transform.position);
            }
            Debug.Log($"[GridManager] Trovati {_treePositions.Count} alberi.");
        }

        // ── API pubblica ─────────────────────────────────────────────────

        public void ShowGrid() => _visible = true;

        public void HideGrid() => _visible = false;

        /// <summary>Snappa worldPos al centro della cella più vicina.</summary>
        public Vector3 SnapToGrid(Vector3 worldPos)
        {
            _lastSnapPos = CellToWorld(WorldToCell(worldPos));
            return _lastSnapPos;
        }

        public bool IsCellFreeAtWorld(Vector3 worldPos) => !_occupied.Contains(WorldToCell(worldPos));

        /// <summary>Ritorna true se la posizione è bloccata da un edificio già piazzato o da un albero.</summary>
        public bool IsPositionBlocked(Vector3 worldPos)
        {
            // 1. Cella occupata da un edificio piazzato via grid
            if (_occupied.Contains(WorldToCell(worldPos))) return true;

            // 1b. Troppo vicino a qualsiasi edificio già piazzato
            float minDist = minBuildingSpacing;
            foreach (var cell in _occupied)
            {
                Vector3 cellWorld = CellToWorld(cell);
                float dx = worldPos.x - cellWorld.x;
                float dz = worldPos.z - cellWorld.z;
                if (dx * dx + dz * dz < minDist * minDist)
                    return true;
            }

            // 2. Campiona 9 punti del footprint reale dell'oggetto (centro + 4 mezzerie + 4 angoli)
            float h = buildingFootprint * 0.48f; // usa la dimensione reale dell'edificio
            Vector3[] pts =
            {
                worldPos,
                worldPos + new Vector3( h, 0,  0),
                worldPos + new Vector3(-h, 0,  0),
                worldPos + new Vector3( 0, 0,  h),
                worldPos + new Vector3( 0, 0, -h),
                worldPos + new Vector3( h, 0,  h),
                worldPos + new Vector3(-h, 0,  h),
                worldPos + new Vector3( h, 0, -h),
                worldPos + new Vector3(-h, 0, -h),
            };

            // Controlla alberi per ogni punto del footprint
            float r2 = treeBlockRadius * treeBlockRadius;
            foreach (var pt in pts)
                foreach (var tp in _treePositions)
                {
                    float dx = pt.x - tp.x;
                    float dz = pt.z - tp.z;
                    if (dx * dx + dz * dz < r2) return true;
                }

            // 3. Collider fisici (case, panche) — box centrato a metà altezza per catturare oggetti a terra
            Vector3 halfExtents = new Vector3(buildingFootprint * 0.49f, 2f, buildingFootprint * 0.49f);
            Vector3 center      = worldPos + Vector3.up * 1f;
            foreach (var col in Physics.OverlapBox(center, halfExtents, Quaternion.identity))
            {
                if (col.isTrigger) continue;
                if (col is TerrainCollider) continue;
                
                // IGNORA le anteprime (Preview) che stiamo draggando
                if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast")) continue;
                if (col.gameObject.CompareTag("EditorOnly")) continue; // Altro modo per ignorare preview

                if (col.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
                if (col.bounds.size.y < 0.1f) continue; 
                return true;
            }

            return false;
        }

        public void OccupyCell(Vector3 worldPos) => _occupied.Add(WorldToCell(worldPos));
        public void FreeCell(Vector3 worldPos)   => _occupied.Remove(WorldToCell(worldPos));

        // ── Math cella ───────────────────────────────────────────────────

        public Vector2Int WorldToCell(Vector3 pos)
            => new(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.z / cellSize));

        public Vector3 CellToWorld(Vector2Int cell)
            => new(cell.x * cellSize, 0f, cell.y * cellSize);

        private void OnRenderObject()
        {
            if (!_visible || _glMat == null) return;

            int cx = Mathf.RoundToInt(_lastSnapPos.x / cellSize);
            int cz = Mathf.RoundToInt(_lastSnapPos.z / cellSize);

            float y  = 0.01f;
            float x0 = (cx - halfGridW) * cellSize;
            float x1 = (cx + halfGridW) * cellSize;
            float z0 = (cz - halfGridH) * cellSize;
            float z1 = (cz + halfGridH) * cellSize;

            _glMat.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 1f, 1f, 0.25f));

            for (int ix = cx - halfGridW; ix <= cx + halfGridW; ix++)
            {
                float x = ix * cellSize;
                GL.Vertex3(x, y, z0);
                GL.Vertex3(x, y, z1);
            }
            for (int iz = cz - halfGridH; iz <= cz + halfGridH; iz++)
            {
                float z = iz * cellSize;
                GL.Vertex3(x0, y, z);
                GL.Vertex3(x1, y, z);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void OnDestroy()
        {
            if (_glMat != null) Destroy(_glMat);
        }
    }
}
