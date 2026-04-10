# Divine Interaction & Social Reaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a reactive social system where villagers observe divine acts, gather to discuss them, and spread the news to others.

**Architecture:** A static `DivineEventManager` broadcasts `DivineEvent` structs. Villagers have a `VillagerSocialReaction` component that listens for these events, manages individual `Loyalty`, and handles social states (Investigating, Gathering, Messenger).

**Tech Stack:** C#, Unity, NavMesh, Unity Events.

---

### Task 1: Divine Event Foundation

**Files:**
- Create: `Assets/_Project/Scripts/Divine/DivineEventManager.cs`

- [ ] **Step 1: Create the DivineEvent struct and Manager**

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace DivinePrototype
{
    public enum DivineEventType { Smite, Repair, Revive }

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
```

- [ ] **Step 2: Verify compilation**

Run: `Check Unity console for errors`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Divine/DivineEventManager.cs
git commit -m "feat(divine): add DivineEventManager and DivineEvent struct"
```

---

### Task 2: Integrate Broadcasting into DivineActionSystem

**Files:**
- Modify: `Assets/_Project/Scripts/Divine/DivineActionSystem.cs`

- [ ] **Step 1: Update Smite, Repair, and Revive to broadcast events**

Modify `OnVillagerTapped` and `OnObjectTapped`:

```csharp
// In OnVillagerTapped case Smite:
DivineEventManager.Broadcast(new DivineEvent { 
    Type = DivineEventType.Smite, 
    Position = villager.transform.position, 
    Target = villager.gameObject, 
    Radius = 20f 
});

// In OnVillagerTapped case Revive:
DivineEventManager.Broadcast(new DivineEvent { 
    Type = DivineEventType.Revive, 
    Position = villager.transform.position, 
    Target = villager.gameObject, 
    Radius = 15f 
});

// In OnObjectTapped case Smite:
DivineEventManager.Broadcast(new DivineEvent { 
    Type = DivineEventType.Smite, 
    Position = obj.transform.position, 
    Target = obj.gameObject, 
    Radius = 15f 
});

// In OnObjectTapped case Repair:
DivineEventManager.Broadcast(new DivineEvent { 
    Type = DivineEventType.Repair, 
    Position = obj.transform.position, 
    Target = obj.gameObject, 
    Radius = 15f 
});
```

- [ ] **Step 2: Verify compilation**

Run: `Check Unity console for errors`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Divine/DivineActionSystem.cs
git commit -m "feat(divine): broadcast divine events from DivineActionSystem"
```

---

### Task 3: Update VillagerController with Loyalty and Social States

**Files:**
- Modify: `Assets/_Project/Scripts/Village/VillagerController.cs`

- [ ] **Step 1: Add Loyalty property and Social States**

```csharp
// Add to enum VillagerState
public enum VillagerState { 
    // ... existing
    Investigating,
    Gathering,
    Messenger,
    Dead
}

// Add to class fields
[Header("Social")]
public float loyalty = 50f; // 0-100
public float perceptionRadius = 20f;

// Add methods to pause/resume work
public void PauseWork() { StopAgent(); }
public void ResumeWork() { GoIdleDirect(); }
```

- [ ] **Step 2: Implement state logic in Update**

Add cases for `Investigating`, `Gathering`, `Messenger` in `Update()` switch. These will be handled by the new `VillagerSocialReaction` component.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Village/VillagerController.cs
git commit -m "feat(village): add loyalty and social states to VillagerController"
```

---

### Task 4: Implement VillagerSocialReaction Component

**Files:**
- Create: `Assets/_Project/Scripts/Village/VillagerSocialReaction.cs`

- [ ] **Step 1: Create the reaction component**

```csharp
using UnityEngine;
using System.Collections;

namespace DivinePrototype
{
    public class VillagerSocialReaction : MonoBehaviour
    {
        private VillagerController _controller;
        private Vector3 _eventPos;
        private DivineEventType _lastEventType;

        void Start()
        {
            _controller = GetComponent<VillagerController>();
            DivineEventManager.OnDivineEvent += HandleDivineEvent;
        }

        void OnDestroy()
        {
            DivineEventManager.OnDivineEvent -= HandleDivineEvent;
        }

        private void HandleDivineEvent(DivineEvent e)
        {
            if (_controller.CurrentState == VillagerController.VillagerState.Dead) return;

            float dist = Vector3.Distance(transform.position, e.Position);
            if (dist <= _controller.perceptionRadius)
            {
                _eventPos = e.Position;
                _lastEventType = e.Type;
                StartCoroutine(ReactToEvent());
            }
        }

        private IEnumerator ReactToEvent()
        {
            _controller.PauseWork();
            // TODO: Add Emoji logic here later
            Debug.Log($"[Social] {name} reacted to {_lastEventType}!");
            
            // Phase 1: Investigating (Move to spot)
            // Implementation detail: we'll add a method to VillagerController to move to a spot without NavMesh if needed
            yield return new WaitForSeconds(2f);
            
            _controller.ResumeWork();
        }
    }
}
```

- [ ] **Step 2: Attach to Villager prefab** (Manual or via script)

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/Village/VillagerSocialReaction.cs
git commit -m "feat(village): add basic VillagerSocialReaction component"
```

---

### Task 5: Implement Gathering and Messenger Logic

**Files:**
- Modify: `Assets/_Project/Scripts/Village/VillagerSocialReaction.cs`

- [ ] **Step 1: Expand ReactToEvent with Gathering and Messenger phases**

Implement the "Gathering" logic where multiple villagers meet at the spot and one is chosen as messenger.

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Scripts/Village/VillagerSocialReaction.cs
git commit -m "feat(social): implement gathering and messenger logic"
```
