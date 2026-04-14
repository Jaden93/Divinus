using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DivinePrototype
{
    /// <summary>
    /// COMPONENTE DISABILITATO. Usare GodHand.cs per il drag & drop.
    /// Tenuto solo per riferimento parametri se necessario.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DivineDraggable : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning($"[DivineDraggable] Rilevato su {name}. Questo script è deprecato. Usare GodHand per il grab.");
            enabled = false;
        }
    }
}
