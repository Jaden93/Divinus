using UnityEngine;
using UnityEditor;

namespace DivinePrototype.Editor
{
    public class DepotSetup : EditorWindow
    {
        [MenuItem("DivineTools/Create Generic Depot Prefab")]
        public static void CreateDepot()
        {
            // 1. Crea l'oggetto base (Cubo placeholder)
            GameObject depotGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            depotGo.name = "GenericDepot";
            depotGo.transform.localScale = new Vector3(2, 1, 2);
            depotGo.transform.position = Vector3.zero;

            // 2. Aggiungi i componenti necessari
            depotGo.AddComponent<GenericDepotController>();
            
            var obstacle = depotGo.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(2f, 2f, 2f);
            obstacle.center = new Vector3(0f, 1f, 0f);

            // 3. Imposta il materiale
            var renderer = depotGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                woodMat.color = new Color(0.4f, 0.25f, 0.1f);
                
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Materials"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
                        AssetDatabase.CreateFolder("Assets", "_Project/Art");
                    AssetDatabase.CreateFolder("Assets/_Project/Art", "Materials");
                }
                AssetDatabase.CreateAsset(woodMat, "Assets/_Project/Art/Materials/M_GenericDepot.mat");
                renderer.sharedMaterial = woodMat;
            }

            // 4. Salva come Prefab
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }
            string path = "Assets/_Project/Prefabs/GenericDepot.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(depotGo, path);
            Object.DestroyImmediate(depotGo);

            // 5. AUTO-COLLEGAMENTO: Cerca WoodDepotActionUI nella scena e assegna il prefab
            var actionUI = Object.FindObjectOfType<WoodDepotActionUI>();
            if (actionUI != null)
            {
                Undo.RecordObject(actionUI, "Assign Depot Prefab");
                actionUI.depotPrefab = prefab;
                EditorUtility.SetDirty(actionUI);
                Debug.Log("[DepotSetup] Prefab assegnato automaticamente a WoodDepotActionUI nella scena.");
            }

            Debug.Log($"[DepotSetup] Prefab creato con successo in: {path}");
            EditorGUIUtility.PingObject(prefab);
        }
    }
}
