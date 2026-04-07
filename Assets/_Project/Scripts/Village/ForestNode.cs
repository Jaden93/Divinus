using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Rappresenta un albero interagibile. Gestisce lo stato (Intact → Depleted)
    /// e la sostituzione visiva con il moncone quando esaurito.
    /// </summary>
    public class ForestNode : MonoBehaviour
    {
        public enum NodeState { Intact, BeingChopped, Depleted }

        [Header("Setup")]
        public GameObject stumpPrefab;      // TreeStump.prefab
        public int woodAmount = 3;          // legna prodotta da questo albero

        [Header("Fall Animation")]
        public float fallDuration = 1.2f;      // durata della caduta
        public float fallAngle   = 80f;        // gradi di rotazione
        public float sinkDelay   = 0.5f;       // pausa prima di sprofondare
        public float sinkDuration = 1.5f;      // durata dello sprofondamento
        public float sinkDepth   = 3f;         // quanto scende nel terreno

        [Header("Fall Damage")]
        public float fallDamageRadius = 2f;    // raggio area impatto chioma
        public float fallDamage       = 40f;   // energia tolta ai villager colpiti

        [Header("Events")]
        public UnityEvent<ForestNode> onDepleted;

        public NodeState State { get; private set; } = NodeState.Intact;

        private VillagerController _assignedVillager;

        /// <summary>
        /// Assegna un popolano a questo albero. Ritorna false se già occupato o esaurito.
        /// </summary>
        public bool TryAssign(VillagerController villager)
        {
            if (State != NodeState.Intact) return false;
            _assignedVillager = villager;
            State = NodeState.BeingChopped;
            return true;
        }

        /// <summary>
        /// Chiamato dal popolano quando finisce di tagliare.
        /// Avvia animazione di caduta e sprofondamento, poi disattiva.
        /// </summary>
        public void Deplete()
        {
            if (State == NodeState.Depleted) return;

            State = NodeState.Depleted;

            // Salva la posizione del boscaiolo per calcolare la direzione di caduta
            Vector3 chopperPos = _assignedVillager != null
                ? _assignedVillager.transform.position
                : transform.position + Vector3.forward;
            _assignedVillager = null;

            if (stumpPrefab != null)
            {
                Instantiate(stumpPrefab, transform.position, transform.rotation);
            }

            onDepleted?.Invoke(this);
            StartCoroutine(FallAndSink(chopperPos));
        }

        private IEnumerator FallAndSink(Vector3 chopperPos)
        {
            // Cade nella direzione OPPOSTA al boscaiolo
            Vector3 awayFromChopper = transform.position - chopperPos;
            awayFromChopper.y = 0f;
            if (awayFromChopper.sqrMagnitude < 0.01f)
                awayFromChopper = Vector3.forward;
            awayFromChopper.Normalize();

            // Asse di rotazione perpendicolare alla direzione di caduta
            Vector3 fallAxis = Vector3.Cross(Vector3.up, awayFromChopper);
            Quaternion startRot = transform.rotation;
            Quaternion endRot = Quaternion.AngleAxis(fallAngle, fallAxis) * startRot;

            // Fase 1: caduta
            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float progress = t / fallDuration;
                float eased = progress * progress; // easing gravitazionale
                transform.rotation = Quaternion.Slerp(startRot, endRot, eased);
                yield return null;
            }
            transform.rotation = endRot;

            // Danno nell'area di impatto della chioma
            Vector3 impactCenter = transform.position + awayFromChopper * 2.5f;
            var hits = Physics.OverlapSphere(impactCenter, fallDamageRadius);
            foreach (var hit in hits)
            {
                var villager = hit.GetComponent<VillagerController>();
                if (villager != null)
                {
                    villager.SetEnergy(villager.Energy - fallDamage);
                    Debug.Log($"[ForestNode] Albero caduto su {villager.name}! Energia -{fallDamage}");
                }
            }

            // Pausa breve a terra
            yield return new WaitForSeconds(sinkDelay);

            // Fase 2: sprofondamento nel terreno
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.down * sinkDepth;
            t = 0f;
            while (t < sinkDuration)
            {
                t += Time.deltaTime;
                float progress = t / sinkDuration;
                float eased = progress * progress * (3f - 2f * progress);
                transform.position = Vector3.Lerp(startPos, endPos, eased);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Libera l'assegnazione senza depletare (es. popolano interrotto).
        /// </summary>
        public void Release()
        {
            if (State == NodeState.BeingChopped)
                State = NodeState.Intact;
            _assignedVillager = null;
        }
    }
}
