using System.Collections;
using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Specializzazione di ResourceNode per le rocce.
    /// Gestisce l'animazione di "sgretolamento" (scale down) quando esaurita.
    /// </summary>
    public class StoneNode : ResourceNode
    {
        [Header("Stone Settings")]
        public float shrinkDuration = 1.0f;    // durata dello sgonfiamento
        public float sinkDepth = 1.0f;       // quanto scende nel terreno

        protected override void Awake()
        {
            base.Awake();
            resourceName = "Stone";
        }

        private void Start()
        {
            // resourceName già impostato in Awake
        }
        protected override void OnDepleteVisuals()
        {
            assignedVillager = null;
            StartCoroutine(ShrinkAndSink());
        }

        private IEnumerator ShrinkAndSink()
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * sinkDepth;
            
            float t = 0f;
            while (t < shrinkDuration)
            {
                t += Time.deltaTime;
                float progress = t / shrinkDuration;
                
                // Sgonfiamento e affondamento graduale
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
