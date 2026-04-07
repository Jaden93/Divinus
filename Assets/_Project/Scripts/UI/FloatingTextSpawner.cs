using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Spawna testi flottanti in screen-space sopra una posizione world.
    /// Usa un singleton leggero — niente DontDestroyOnLoad.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance { get; private set; }

        [Header("Canvas root (auto-trovato se null)")]
        public Canvas rootCanvas;
        public Camera mainCamera;

        private void Awake()
        {
            Instance = this;
            if (rootCanvas  == null) rootCanvas  = FindObjectOfType<Canvas>();
            if (mainCamera  == null) mainCamera  = Camera.main;
        }

        /// <summary>
        /// worldPos: posizione 3D da cui parte il testo.
        /// text: stringa da mostrare.
        /// color: colore del testo.
        /// </summary>
        public void Spawn(string text, Vector3 worldPos, Color color)
        {
            if (rootCanvas == null || mainCamera == null) return;
            StartCoroutine(AnimateText(text, worldPos, color));
        }

        private IEnumerator AnimateText(string text, Vector3 worldPos, Color color)
        {
            // Crea il GameObject testo nel canvas
            var go = new GameObject("FloatingText");
            go.transform.SetParent(rootCanvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            go.AddComponent<CanvasRenderer>();
            var txt       = go.AddComponent<Text>();
            txt.text      = text;
            txt.fontSize  = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color     = color;
            txt.raycastTarget = false;

            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) txt.font = f;

            float duration  = 1.4f;
            float elapsed   = 0f;
            float riseSpeed = 60f; // pixel/s verso l'alto

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Converti posizione world → screen ogni frame (il GO può muoversi)
                Vector3 screen = mainCamera.WorldToScreenPoint(worldPos);
                screen.y += riseSpeed * elapsed;

                // Converti screen → local canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    screen,
                    rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                    out Vector2 local
                );
                rt.localPosition = local;

                // Fade out nell'ultima metà
                float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
                txt.color = new Color(color.r, color.g, color.b, alpha);

                yield return null;
            }

            Destroy(go);
        }
    }
}
