using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Handles the visual impact of the villager on the terrain (Corruption vs Sanctification).
    /// </summary>
    public class VillagerGroundEffectController : MonoBehaviour
    {
        private VillagerController _controller;
        private ParticleSystem _currentTrail;
        
        [Header("Settings")]
        public float loyaltyThresholdHoly = 80f;
        public float loyaltyThresholdDark = 10f;
        public float spawnInterval = 0.4f;
        private float _timer;

        void Start()
        {
            _controller = GetComponent<VillagerController>();
        }

        void Update()
        {
            if (_controller == null || _controller.CurrentState == VillagerController.VillagerState.Dead) return;
            if (_controller.CurrentState != VillagerController.VillagerState.Walking && 
                _controller.CurrentState != VillagerController.VillagerState.Idle) return;

            float loyalty = _controller.loyalty;
            
            if (loyalty >= loyaltyThresholdHoly || loyalty <= loyaltyThresholdDark)
            {
                _timer += Time.deltaTime;
                if (_timer >= spawnInterval)
                {
                    _timer = 0f;
                    LeaveMark(loyalty >= loyaltyThresholdHoly);
                }
            }
        }

        void LeaveMark(bool isHoly)
        {
            // For now, we spawn a temporary "Blob" or "Mark" object
            // Ideally this would interact with a global terrain texture/map
            
            GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = isHoly ? "HolyMark" : "DarkMark";
            mark.transform.position = transform.position + Vector3.up * 0.05f;
            mark.transform.rotation = Quaternion.Euler(90, 0, 0);
            mark.transform.localScale = Vector3.one * 1.5f;
            
            // Remove collider
            Destroy(mark.GetComponent<Collider>());
            
            // Apply Material
            Renderer rend = mark.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Transparent/Diffuse"));
            mat.color = isHoly ? new Color(1f, 1f, 0.7f, 0.4f) : new Color(0.2f, 0f, 0.3f, 0.6f);
            rend.material = mat;
            
            // Destroy after some time
            Destroy(mark, 10f);
            
            // Fade out logic
            StartCoroutine(FadeOut(mark, rend, 10f));
        }

        System.Collections.IEnumerator FadeOut(GameObject go, Renderer rend, float duration)
        {
            float elapsed = 0;
            Color startColor = rend.material.color;
            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0, elapsed / duration);
                rend.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }
    }
}
