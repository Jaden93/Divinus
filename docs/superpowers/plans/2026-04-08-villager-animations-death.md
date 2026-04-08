# Villager Animations & Smite Death Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate "Start Walking" and "Dying" animations for the villager, including a new death state triggered by the Smite ability.

**Architecture:** 
- Update `VillagerController.VillagerState` enum to include `Dead`.
- Add parameters to the Animator Controller to handle "Start Walking" (trigger) and "Dying" (trigger).
- Modify `VillagerController` to transition to `Dead` state when receiving damage from Smite.
- Update `DivineActionSystem` to correctly interface with the villager's new death logic.

**Tech Stack:** Unity, C#, Animator Controller.

---

### Task 1: Update VillagerController State and Logic

**Files:**
- Modify: `Assets/_Project/Scripts/Village/VillagerController.cs`

- [ ] **Step 1: Add Dead state to VillagerState enum**

```csharp
public enum VillagerState
{
    Idle, Walking, ChoppingWood, CarryingWood,
    GoingToSleep, Sleeping,
    PickingUpAxe, Resting,
    GoingToBench, Sitting,
    Dead // New state
}
```

- [ ] **Step 2: Add Animator parameter hashes**

```csharp
private static readonly int ParamStartWalk = Animator.StringToHash("startWalking");
private static readonly int ParamDying     = Animator.StringToHash("dying");
```

- [ ] **Step 3: Implement Die() method**

```csharp
public void Die()
{
    if (CurrentState == VillagerState.Dead) return;
    
    // Release resources
    if (_targetNode != null) _targetNode.Release();
    if (_targetBench != null) _targetBench.Vacate();
    if (_targetHouse != null) _targetHouse.Vacate();
    
    _targetNode = null;
    _targetBench = null;
    _targetHouse = null;
    
    StopAgent();
    CurrentState = VillagerState.Dead;
    
    if (_animator != null)
    {
        _animator.SetTrigger(ParamDying);
    }
    
    Debug.Log($"[VillagerController] {name} is DEAD.");
}
```

- [ ] **Step 4: Update GoWalking() to trigger Start Walking animation**

```csharp
private void GoWalking()
{
    if (!TryGetRandomNavMeshPoint(out Vector3 dest))
    {
        GoIdleDirect();
        return;
    }
    _walkTarget  = dest;
    CurrentState = VillagerState.Walking;
    
    if (_animator != null)
    {
        _animator.SetTrigger(ParamStartWalk);
    }
    
    SetAnim(true, false);
    MoveTo(Flat(dest));
}
```

- [ ] **Step 5: Prevent updates if Dead**

Add to start of `Update()`:
```csharp
if (CurrentState == VillagerState.Dead) return;
```

---

### Task 2: Update DivineActionSystem to trigger Death

**Files:**
- Modify: `Assets/_Project/Scripts/Divine/DivineActionSystem.cs`

- [ ] **Step 1: Update OnVillagerTapped for Smite**

```csharp
private void OnVillagerTapped(VillagerController villager)
{
    switch (PendingPower)
    {
        case DivinePower.Smite:
            LightningStrike.Spawn(villager.transform.position + Vector3.up * 0.5f);
            if (faithSystem != null) faithSystem.AddFaith(-15f);
            
            // Trigger death
            villager.Die();
            
            StartCoroutine(FlashVillager(villager, new Color(1f, 0.9f, 0.1f), 0.3f));
            Debug.Log($"[DivineActionSystem] Smite villager: {villager.name}");
            break;
            // ... rest of cases
```

---

### Task 3: Animator Controller Setup (Manual Verification Required)

**Files:**
- Note: This involves Unity Editor changes which cannot be automated via code edits directly, but the instructions are provided for the user.

- [ ] **Step 1: Add Animator Parameters**
  - Add a Trigger named `startWalking`.
  - Add a Trigger named `dying`.

- [ ] **Step 2: Setup Transitions**
  - Create a transition from `Any State` to `Dying` state (using `Dying.fbx` clip) triggered by `dying`.
  - Update `Idle` to `Walking` transition to potentially include `Start Walking.fbx` as an intermediate state if desired, or simply use the trigger to blend.

---

### Task 4: Revive Logic Update

**Files:**
- Modify: `Assets/_Project/Scripts/Village/VillagerController.cs`
- Modify: `Assets/_Project/Scripts/Divine/DivineActionSystem.cs`

- [ ] **Step 1: Implement Revive() in VillagerController**

```csharp
public void Revive(float energyPercent)
{
    if (CurrentState != VillagerState.Dead) return;
    
    Energy = maxEnergy * energyPercent;
    SetVisibility(true); // Ensure visible if it was hidden
    GoIdleDirect();
    Debug.Log($"[VillagerController] {name} REVIVED.");
}
```

- [ ] **Step 2: Update DivineActionSystem.OnVillagerTapped for Revive**

```csharp
case DivinePower.Revive:
    if (villager.CurrentState == VillagerState.Dead)
    {
        if (faithSystem != null) faithSystem.AddFaith(-15f);
        villager.Revive(0.5f);
        StartCoroutine(FlashVillager(villager, new Color(0.8f, 1f, 0.8f), 1.0f));
        Debug.Log($"[DivineActionSystem] Revive villager: {villager.name}");
        ClearPendingPower();
    }
    break;
```
