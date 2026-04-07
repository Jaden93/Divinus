using UnityEngine;
using UnityEngine.InputSystem;

namespace DivinePrototype
{
    /// <summary>
    /// Camera con zoom avanti/indietro e movimento WASD per PC.
    /// Scroll mouse + WASD in editor, pinch a due dita su mobile.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom")]
        public float scrollZoomSpeed = 3f;
        public float minHeight = 2f;
        public float maxHeight = 20f;

        [Header("Pan WASD (solo editor/PC)")]
        public float panSpeed = 10f;

        [Header("Pinch")]
        public float pinchSensitivity = 0.04f;

        private float _prevPinchDist;

        private void Update()
        {
            HandleScrollZoom();
            HandleWASD();
            HandlePinchZoom();
        }

        private void HandleScrollZoom()
        {
            if (Mouse.current == null) return;
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;
            // Ogni notch della rotella muove la camera di esattamente scrollZoomSpeed unità
            ApplyZoom(Mathf.Sign(scroll) * scrollZoomSpeed);
        }

        private void HandleWASD()
        {
            if (Keyboard.current == null) return;

            var kb = Keyboard.current;
            float x = 0f, z = 0f;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    z =  1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  z = -1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x =  1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x = -1f;

            if (x == 0f && z == 0f) return;

            // Muove sul piano XZ ignorando la componente Y della camera
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            transform.position += (forward * z + right * x) * panSpeed * Time.deltaTime;
        }

        private void HandlePinchZoom()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return;

            // Trova i primi due touch attivi
            var t0 = touchscreen.touches[0];
            var t1 = touchscreen.touches[1];

            var phase0 = t0.phase.ReadValue();
            var phase1 = t1.phase.ReadValue();

            bool t0Active = phase0 != UnityEngine.InputSystem.TouchPhase.None &&
                            phase0 != UnityEngine.InputSystem.TouchPhase.Ended &&
                            phase0 != UnityEngine.InputSystem.TouchPhase.Canceled;
            bool t1Active = phase1 != UnityEngine.InputSystem.TouchPhase.None &&
                            phase1 != UnityEngine.InputSystem.TouchPhase.Ended &&
                            phase1 != UnityEngine.InputSystem.TouchPhase.Canceled;

            if (!t0Active || !t1Active) return;

            float dist = Vector2.Distance(t0.position.ReadValue(), t1.position.ReadValue());

            if (phase0 == UnityEngine.InputSystem.TouchPhase.Began ||
                phase1 == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _prevPinchDist = dist;
                return;
            }

            float delta = dist - _prevPinchDist;
            _prevPinchDist = dist;

            ApplyZoom(delta * pinchSensitivity);
        }

        /// <summary>
        /// Muove la camera lungo il suo asse forward. Clampa per altezza Y
        /// scalando l'intero vettore per evitare drift XZ ai limiti.
        /// </summary>
        private void ApplyZoom(float amount)
        {
            Vector3 move = transform.forward * amount;
            Vector3 newPos = transform.position + move;

            // Se la Y sfora i limiti, scala tutto il movimento al limite esatto
            if (Mathf.Abs(move.y) > 0.0001f)
            {
                if (newPos.y > maxHeight)
                {
                    float scale = (maxHeight - transform.position.y) / move.y;
                    move *= scale;
                    newPos = transform.position + move;
                }
                else if (newPos.y < minHeight)
                {
                    float scale = (minHeight - transform.position.y) / move.y;
                    move *= scale;
                    newPos = transform.position + move;
                }
            }

            transform.position = newPos;
        }
    }
}
