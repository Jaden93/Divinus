using UnityEngine;

namespace DivinePrototype
{
    /// <summary>
    /// Panca su cui il villager può sedersi per recuperare energia.
    /// Gestisce un singolo slot occupante alla volta.
    /// </summary>
    public class BenchController : MonoBehaviour
    {
        [Header("Seduta")]
        public float energyRecoveryPerSecond = 25f;  // più veloce del riposo in casa
        public float sitDuration             = 6f;   // secondi sulla panca

        [Header("Posizione seduta")]
        public Vector3 sitOffset = new Vector3(0f, 0.5f, 0f); // dove si posiziona il villager

        public bool  IsOccupied  { get; private set; } = false;
        public Transform SitPoint => transform;

        private VillagerController _occupant;

        /// <summary>Registra il villager come occupante. Ritorna false se già occupata.</summary>
        public bool TryOccupy(VillagerController villager)
        {
            if (IsOccupied) return false;
            IsOccupied = true;
            _occupant  = villager;
            return true;
        }

        /// <summary>Libera la panca quando il villager si alza.</summary>
        public void Vacate()
        {
            IsOccupied = false;
            _occupant  = null;
        }

        /// <summary>Posizione world dove il villager si siede.</summary>
        public Vector3 GetSitPosition()
        {
            return transform.position + transform.rotation * sitOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(GetSitPosition(), 0.2f);
        }
    }
}
