using UnityEngine;
using UnityEngine.Events;

namespace DivinePrototype
{
    /// <summary>
    /// Gestisce la variabile globale di fede del villaggio (0-100).
    /// Altri sistemi si iscrivono a OnFaithChanged per reagire.
    /// </summary>
    public class VillageFaithSystem : MonoBehaviour
    {
        [Header("Fede")]
        [Range(0f, 100f)]
        public float startingFaith = 20f;

        [Header("Events")]
        public UnityEvent<float> onFaithChanged;   // valore corrente 0-100
        public UnityEvent onVictory;               // faith >= 100

        public float Faith { get; private set; }

        private bool _victoryTriggered = false;

        private void Awake()
        {
            Faith = Mathf.Clamp(startingFaith, 0f, 100f);
        }

        private void Start()
        {
            // Notifica la UI del valore iniziale
            onFaithChanged?.Invoke(Faith);
        }

        public void AddFaith(float amount)
        {
            SetFaith(Faith + amount);
        }

        public void RemoveFaith(float amount)
        {
            SetFaith(Faith - amount);
        }

        private void SetFaith(float value)
        {
            Faith = Mathf.Clamp(value, 0f, 100f);
            onFaithChanged?.Invoke(Faith);

            if (!_victoryTriggered && Faith >= 100f)
            {
                _victoryTriggered = true;
                onVictory?.Invoke();
            }
        }
    }
}
