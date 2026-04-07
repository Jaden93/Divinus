using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Tiene traccia dello stato globale dell'MVP 0.
    /// Altri sistemi leggono e scrivono qui tramite riferimento Inspector.
    /// </summary>
    public class GameStateSystem : MonoBehaviour
    {
        [Header("Stato corrente (read-only in Play)")]
        [SerializeField] private bool _hasVillager;
        [SerializeField] private bool _hasAxe;
        [SerializeField] private int  _woodCount;
        [SerializeField] private bool _hasHouse;

        [Header("Events")]
        public UnityEvent onVillagerCreated;
        public UnityEvent onAxeGranted;
        public UnityEvent<int> onWoodChanged;   // parametro: totale legna
        public UnityEvent onHouseBuilt;

        // ── Proprietà pubbliche ──────────────────────────────────────────

        public bool HasVillager
        {
            get => _hasVillager;
            set
            {
                if (_hasVillager == value) return;
                _hasVillager = value;
                if (value) onVillagerCreated?.Invoke();
            }
        }

        public bool HasAxe
        {
            get => _hasAxe;
            set
            {
                if (_hasAxe == value) return;
                _hasAxe = value;
                if (value) onAxeGranted?.Invoke();
            }
        }

        public int WoodCount
        {
            get => _woodCount;
            set
            {
                if (_woodCount == value) return;
                _woodCount = value;
                onWoodChanged?.Invoke(_woodCount);
            }
        }

        public bool HasHouse
        {
            get => _hasHouse;
            set
            {
                if (_hasHouse == value) return;
                _hasHouse = value;
                if (value) onHouseBuilt?.Invoke();
            }
        }
    }
}
