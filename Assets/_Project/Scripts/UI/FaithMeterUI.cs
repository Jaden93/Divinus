using UnityEngine;
using UnityEngine.UI;

namespace DivinePrototype
{
    /// <summary>
    /// Aggiorna la barra della fede in risposta agli eventi di VillageFaithSystem.
    /// </summary>
    public class FaithMeterUI : MonoBehaviour
    {
        [Header("Riferimenti UI")]
        public Slider faithSlider;
        public Text faithLabel;

        /// <summary>Chiamato da VillageFaithSystem.onFaithChanged.</summary>
        public void OnFaithChanged(float value)
        {
            if (faithSlider != null)
                faithSlider.value = value / 100f;

            if (faithLabel != null)
                faithLabel.text = "Fede: " + Mathf.RoundToInt(value) + "%";
        }
    }
}
