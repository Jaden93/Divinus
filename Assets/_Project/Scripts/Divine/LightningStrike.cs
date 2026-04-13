using System.Collections;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Spawna un fulmine zigzag dal cielo verso un punto bersaglio.
    /// Si auto-distrugge dopo l'animazione.
    /// Uso: LightningStrike.Spawn(targetPosition)
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningStrike : MonoBehaviour
    {
        [Header("Aspetto")]
        public float   strikeHeight   = 12f;
        public int     segments       = 8;
        public float   zigzagAmount   = 0.6f;
        public float   lineWidth      = 0.08f;
        public Color   colorStart     = new Color(1f, 0.95f, 0.4f, 1f);
        public Color   colorEnd       = new Color(1f, 0.5f, 0.1f, 0f);

        [Header("Timing")]
        public float   flashDuration  = 0.08f;
        public float   fadeDuration   = 0.18f;

        private LineRenderer _lr;
        private Vector3      _target;
        public GameObject impactVFXPrefab;


        // ── Static factory ───────────────────────────────────────────────

public static LightningStrike Spawn(Vector3 target, GameObject impactPrefab = null)
        {
            var go = new GameObject("LightningStrike");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace      = true;
            lr.positionCount      = 0;

            // Material: use Sprites/Default (unlit, bright)
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.SetColor("_Color", Color.white);
            lr.material = mat;
            lr.startWidth = 0.08f;
            lr.endWidth   = 0.02f;
            lr.colorGradient = MakeGradient(
                new Color(1f, 0.95f, 0.4f, 1f),
                new Color(1f, 0.5f,  0.1f, 0f));

            var strike = go.AddComponent<LightningStrike>();
            strike._target = target;
            strike.impactVFXPrefab = impactPrefab;
            go.transform.position = target;
            return strike;
        }

        void Start()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startWidth    = lineWidth;
            _lr.endWidth      = lineWidth * 0.25f;
            _lr.colorGradient = MakeGradient(colorStart, colorEnd);

            if (_lr.sharedMaterial == null || _lr.sharedMaterial.name == "Default-Line")
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                _lr.material = mat;
            }

            StartCoroutine(DoStrike());
        }

private IEnumerator DoStrike()
        {
            // Build zigzag path from sky to target
            Vector3 start = _target + Vector3.up * strikeHeight;
            Vector3[] pts = new Vector3[segments + 1];
            pts[0] = start;
            pts[segments] = _target;

            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                Vector3 straight = Vector3.Lerp(start, _target, t);
                float jitter = (1f - t) * zigzagAmount;
                straight += new Vector3(
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter * 0.3f, jitter * 0.3f),
                    Random.Range(-jitter, jitter));
                pts[i] = straight;
            }

            _lr.positionCount = pts.Length;
            _lr.SetPositions(pts);

            if (impactVFXPrefab != null)
            {
                var vfx = Instantiate(impactVFXPrefab, _target, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            // Flash: keep visible for flashDuration
            yield return new WaitForSeconds(flashDuration);

            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);
                var g = MakeGradient(
                    new Color(colorStart.r, colorStart.g, colorStart.b, alpha),
                    new Color(colorEnd.r,   colorEnd.g,   colorEnd.b,   0f));
                _lr.colorGradient = g;
                yield return null;
            }

            Destroy(gameObject);
        }

        private static Gradient MakeGradient(Color c0, Color c1)
        {
            var g = new Gradient();
            g.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(c0, 0f),
                new GradientColorKey(c1, 1f)
            };
            g.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(c0.a, 0f),
                new GradientAlphaKey(c1.a, 1f)
            };
            return g;
        }
    }
}
