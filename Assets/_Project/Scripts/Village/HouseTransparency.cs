using System.Collections.Generic;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Rende le pareti della casa semi-trasparenti quando la camera si avvicina abbastanza.
    /// Aggiungere al prefab della casa.
    /// Assegnare i renderer delle pareti in wallRenderers (o verranno presi tutti i child renderer).
    /// </summary>
    public class HouseTransparency : MonoBehaviour
    {
        [Header("Distanza")]
        [Tooltip("Sotto questa distanza le pareti diventano trasparenti.")]
        public float enterDistance = 5f;
        [Tooltip("Sopra questa distanza le pareti tornano opache (isteresi per evitare sfarfallio).")]
        public float exitDistance = 6.5f;

        [Header("Trasparenza")]
        [Range(0f, 1f)]
        public float wallAlpha = 0.25f;

        [Header("Renderer pareti")]
        [Tooltip("Assegna qui i renderer delle pareti. Se vuoto usa tutti i child renderer.")]
        public Renderer[] wallRenderers;

        private bool _isTransparent;
        private readonly List<Material[]> _originalMaterials = new();
        private readonly List<Material[]> _instanceMaterials = new();

        private void Awake()
        {
            if (wallRenderers == null || wallRenderers.Length == 0)
                wallRenderers = GetComponentsInChildren<Renderer>();

            // Istanzia i materiali per non modificare gli asset shared
            foreach (var r in wallRenderers)
            {
                _originalMaterials.Add(r.sharedMaterials);
                // renderer.materials crea istanze
                r.materials = r.sharedMaterials;
                _instanceMaterials.Add(r.materials);
            }
        }

        private void Update()
        {
            if (Camera.main == null) return;

            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);

            if (!_isTransparent && dist < enterDistance)
                SetTransparent(true);
            else if (_isTransparent && dist > exitDistance)
                SetTransparent(false);
        }

        private void SetTransparent(bool transparent)
        {
            _isTransparent = transparent;

            for (int i = 0; i < wallRenderers.Length; i++)
            {
                foreach (var mat in _instanceMaterials[i])
                {
                    if (transparent)
                        MakeTransparent(mat, wallAlpha);
                    else
                        MakeOpaque(mat);
                }
            }
        }

        // Switcha lo Standard shader in modalità Fade/Transparent a runtime
        private static void MakeTransparent(Material mat, float alpha)
        {
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }

        private static void MakeOpaque(Material mat)
        {
            mat.SetFloat("_Mode", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
            Color c = mat.color;
            c.a = 1f;
            mat.color = c;
        }

        private void OnDestroy()
        {
            // Pulisce i materiali istanziati
            for (int i = 0; i < wallRenderers.Length; i++)
            {
                foreach (var mat in _instanceMaterials[i])
                    Destroy(mat);
            }
        }
    }
}
