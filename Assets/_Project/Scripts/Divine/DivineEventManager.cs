using UnityEngine;
using System;
using System.Collections.Generic;

namespace DivinePrototype
{
    public enum DivineEventType { None, Smite, Repair, Revive, Messenger }

    public struct DivineEvent
    {
        public DivineEventType Type;
        public Vector3 Position;
        public GameObject Target;
        public float Radius;
    }

    public static class DivineEventManager
    {
        public static event Action<DivineEvent> OnDivineEvent;

        public static void Broadcast(DivineEvent e)
        {
            Debug.Log($"[DivineEventManager] Broadcasting {e.Type} at {e.Position}");
            OnDivineEvent?.Invoke(e);
        }
    }
}
