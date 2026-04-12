using UnityEngine;

namespace DivinePrototype
{
    public class VillagerAuraController : MonoBehaviour
    {
        private VillagerController _controller;
        private Light _auraLight;
        private ParticleSystem _auraParticles;
        
        [Header("Settings")]
        public Color holyColor = new Color(1f, 0.9f, 0.4f); // Golden yellow
        public Color darkColor = new Color(0.4f, 0.05f, 0.6f); // Deep Purple/Dark
        
        void Start()
        {
            _controller = GetComponent<VillagerController>();
            SetupAura();
        }

        void SetupAura()
        {
            // Create a light for the aura effect
            GameObject lightGO = new GameObject("AuraLight");
            lightGO.transform.SetParent(transform);
            lightGO.transform.localPosition = Vector3.up * 0.5f;
            
            _auraLight = lightGO.AddComponent<Light>();
            _auraLight.type = LightType.Point;
            _auraLight.range = 3f;
            _auraLight.intensity = 0f;
            _auraLight.shadows = LightShadows.None;
        }

        void Update()
        {
            if (_controller == null || _controller.CurrentState == VillagerController.VillagerState.Dead)
            {
                if (_auraLight != null) _auraLight.intensity = 0f;
                return;
            }

            float loyalty = _controller.loyalty;
            UpdateAuraVisuals(loyalty);
        }

        void UpdateAuraVisuals(float loyalty)
        {
            float targetIntensity = 0f;
            Color targetColor = holyColor;
            float targetRange = 3f;

            if (loyalty >= 80f)
            {
                targetColor = holyColor;
                if (loyalty >= 99f) { targetIntensity = 4.0f; targetRange = 5f; } // Angel
                else if (loyalty >= 95f) targetIntensity = 2.5f;                // Protector
                else if (loyalty >= 90f) targetIntensity = 1.5f;                // Saint
                else targetIntensity = 0.8f;                                   // Blessed
            }
            else if (loyalty <= 10f)
            {
                targetColor = darkColor;
                if (loyalty <= 1f) { targetIntensity = 3.5f; targetRange = 4.5f; } // Dark Angel
                else if (loyalty <= 5f) targetIntensity = 2.0f;                  // Heretic Leader
                else targetIntensity = 1.0f;                                    // Skeptic
            }

            if (_auraLight != null)
            {
                _auraLight.intensity = Mathf.Lerp(_auraLight.intensity, targetIntensity, Time.deltaTime * 2f);
                _auraLight.color = Color.Lerp(_auraLight.color, targetColor, Time.deltaTime * 5f);
                _auraLight.range = Mathf.Lerp(_auraLight.range, targetRange, Time.deltaTime * 2f);
            }
        }
    }
}
