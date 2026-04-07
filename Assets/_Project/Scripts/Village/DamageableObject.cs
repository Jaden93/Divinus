using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    public class DamageableObject : MonoBehaviour
    {
        public enum DamageState { Intact, Damaged, Destroyed }

        [Header("Health")]
        [SerializeField] private int maxHits = 2;
        private int _hits;
        public DamageState CurrentState { get; private set; } = DamageState.Intact;

        [Header("Renderers")]
        [Tooltip("Renderers whose materials will be swapped on damage. Auto-collected if empty.")]
        [SerializeField] private Renderer[] targetRenderers;

        [Header("Materials")]
        [Tooltip("Materials to show when Damaged (one per renderer, or just one for all)")]
        [SerializeField] private Material[] damagedMaterials;
        [Tooltip("Materials to show briefly before hiding when Destroyed")]
        [SerializeField] private Material[] destroyedMaterials;
        private Material[][] _originalMats;

        [Header("Tilt on damage")]
        [SerializeField] private float maxTiltDeg = 12f;

        [Header("Destruction")]
        [Tooltip("Optional prefab spawned at this position when destroyed (rubble, debris pile)")]
        [SerializeField] private GameObject destroyedPrefab;
        [Tooltip("Seconds before hiding renderers after destruction")]
        [SerializeField] private float hideDelay = 0.4f;

        [Header("VFX")]
        [SerializeField] private ParticleSystem smokeFX;
        [SerializeField] private ParticleSystem debrisFX;

        [Header("Events")]
        public UnityEvent onDamaged;
        public UnityEvent onDestroyed;

        void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>();

            _originalMats = new Material[targetRenderers.Length][];
            for (int i = 0; i < targetRenderers.Length; i++)
                _originalMats[i] = targetRenderers[i].sharedMaterials;
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// Called by DivineActionSystem when the god strikes this object.
        public void TakeDamage()
        {
            if (CurrentState == DamageState.Destroyed) return;
            _hits++;
            SetState(_hits >= maxHits ? DamageState.Destroyed : DamageState.Damaged);
        }

        /// Restore the object to its original state (e.g. divine blessing).
        public void Repair()
        {
            _hits = 0;
            SetState(DamageState.Intact);
        }

        public void SetState(DamageState state)
        {
            CurrentState = state;

            switch (state)
            {
                case DamageState.Intact:
                    for (int i = 0; i < targetRenderers.Length; i++)
                        targetRenderers[i].sharedMaterials = _originalMats[i];
                    transform.localRotation = Quaternion.identity;
                    break;

                case DamageState.Damaged:
                    ApplyMaterials(damagedMaterials);
                    StartCoroutine(TiltRoutine());
                    PlayFX(smokeFX);
                    onDamaged?.Invoke();
                    break;

                case DamageState.Destroyed:
                    PlayFX(smokeFX);
                    PlayFX(debrisFX);
                    if (destroyedPrefab != null)
                        Instantiate(destroyedPrefab, transform.position, transform.rotation);
                    StartCoroutine(DestroyRoutine());
                    onDestroyed?.Invoke();
                    break;
            }
        }

        // ── Private ─────────────────────────────────────────────────────────

        private void ApplyMaterials(Material[] mats)
        {
            if (mats == null || mats.Length == 0) return;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                int mi = Mathf.Min(i, mats.Length - 1);
                if (mats[mi] != null)
                    targetRenderers[i].material = mats[mi];
            }
        }

        private IEnumerator TiltRoutine()
        {
            Quaternion start  = transform.localRotation;
            Quaternion target = Quaternion.Euler(
                Random.Range(-maxTiltDeg, maxTiltDeg), 0f,
                Random.Range(-maxTiltDeg, maxTiltDeg));
            for (float t = 0f; t < 1f; t += Time.deltaTime * 3f)
            {
                transform.localRotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }
            transform.localRotation = target;
        }

        private IEnumerator DestroyRoutine()
        {
            ApplyMaterials(destroyedMaterials);
            yield return new WaitForSeconds(hideDelay);
            foreach (var r in targetRenderers)
                if (r != null) r.enabled = false;
        }

        private void PlayFX(ParticleSystem ps)
        {
            if (ps != null && !ps.isPlaying) ps.Play();
        }
    }
}
