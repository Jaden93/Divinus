using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Crea un effetto particellare dorato procedurale attorno al bersaglio.
    /// Si auto-distrugge dopo la durata.
    /// </summary>
    public class ReviveVFX : MonoBehaviour
    {
        public float duration = 2.0f;
        public Color goldColor = new Color(1f, 0.85f, 0f, 1f);

        public static void Spawn(Vector3 position)
        {
            GameObject go = new GameObject("ReviveVFX_Procedural");
            go.transform.position = position + Vector3.up * 0.1f;
            go.AddComponent<ReviveVFX>();
        }

        void Start()
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            
            // Set up main module
            var main = ps.main;
            main.duration = duration;
            main.startColor = goldColor;
            main.startSize = 0.2f;
            main.startSpeed = 2f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.loop = false;

            // Set up emission
            var emission = ps.emission;
            emission.rateOverTime = 40;

            // Set up shape
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            shape.rotation = new Vector3(-90, 0, 0);

            // Set up color over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(goldColor, 0.0f), new GradientColorKey(goldColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            ps.Play();
            Destroy(gameObject, duration + 0.5f);
        }
    }
}
