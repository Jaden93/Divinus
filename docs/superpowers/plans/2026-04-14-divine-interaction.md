# Divine Interaction (Tap & Drag) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementare un sistema di interazione "Dio" che permetta di selezionare villager con un tap (mostrando icone e pensieri) e di lanciarli fisicamente con un drag veloce (causando danni e impatti).

**Architecture:** 
- `GodHand.cs` gestirà la logica di Drag & Throw calcolando la velocità di rilascio.
- `DivineSelectionSystem.cs` gestirà il Tap e l'attivazione della luce divina.
- `VillagerController.cs` riceverà uno stato `DivineProjectile` per gestire la fisica e le collisioni post-lancio.
- UI Overhead tramite Canvas World Space collegato al villager selezionato.

**Tech Stack:** Unity, C#, NavMesh, Rigidbody Physics.

---

### Task 1: Fisica del Lancio e Stato Proiettile

**Files:**
- Modify: `Assets/_Project/Scripts/Village/VillagerController.cs`
- Modify: `Assets/_Project/Scripts/Input/GodHand.cs`

- [ ] **Step 1: Aggiungere stato DivineProjectile e logica collisioni in VillagerController**
Aggiungere `DivineProjectile` all'enum `VillagerState` e implementare `OnCollisionEnter`.

```csharp
// In VillagerController.cs
public enum VillagerState {
    // ... esistenti ...
    DivineProjectile
}

private void OnCollisionEnter(Collision collision) {
    if (CurrentState != VillagerState.DivineProjectile) return;
    
    float impactVelocity = collision.relativeVelocity.magnitude;
    if (impactVelocity > 5f) {
        ModifyHealth(-impactVelocity * 2f); // Danno da impatto
        
        // Interazione con alberi
        var node = collision.collider.GetComponentInParent<ResourceNode>();
        if (node != null) {
            // Logica per "scuotere" o danneggiare l'albero
            node.TakeResource(1); 
        }
        
        // Interazione con altri villager
        var other = collision.collider.GetComponentInParent<VillagerController>();
        if (other != null) {
            other.ModifyHealth(-impactVelocity);
            other.ModifyLoyalty(-10f);
        }
    }
    
    // Ritorna a Idle dopo un breve delay o quando la velocità è bassa
    if (impactVelocity < 1f) GoIdleDirect();
}
```

- [ ] **Step 2: Aggiornare GodHand per calcolare la velocità di lancio**
Modificare `Release()` per applicare forza fisica se il movimento è veloce.

```csharp
// In GodHand.cs
private Vector2 _lastInputPos;
private Vector2 _dragVelocity;

private void UpdateHeldObjectPosition() {
    Vector2 currentPos = GetInputScreenPos();
    _dragVelocity = (currentPos - _lastInputPos) / Time.deltaTime;
    _lastInputPos = currentPos;
    // ... logica esistente ...
}

private void Release() {
    if (_heldObject == null) return;
    
    float throwThreshold = 500f; // Soglia pixel/secondo
    if (_dragVelocity.magnitude > throwThreshold) {
        var rb = _heldObject.GetComponent<Rigidbody>();
        var villager = _heldObject.GetComponent<VillagerController>();
        
        if (rb != null && villager != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
            villager.SetState(VillagerController.VillagerState.DivineProjectile);
            
            Vector3 throwDir = _camera.ScreenPointToRay(GetInputScreenPos()).direction;
            rb.AddForce(throwDir * (_dragVelocity.magnitude * 0.05f), ForceMode.Impulse);
        }
    } else {
        // Logica di deposito "gentile" esistente
    }
    _heldObject = null;
}
```

- [ ] **Step 3: Test manuale in Unity**
Lanciare un villager contro un albero e verificare che la salute scenda e l'albero reagisca.

---

### Task 2: Selezione Divina e Spotlight

**Files:**
- Create: `Assets/_Project/Scripts/Divine/DivineSelectionSystem.cs`
- Modify: `Assets/_Project/Scripts/Input/TouchInputSystem.cs`

- [ ] **Step 1: Creare DivineSelectionSystem**
Gestisce il target selezionato e attiva la luce.

```csharp
using UnityEngine;

namespace DivinePrototype {
    public class DivineSelectionSystem : MonoBehaviour {
        public Light spotlight;
        private VillagerController _selectedVillager;

        public void SelectVillager(VillagerController villager) {
            if (_selectedVillager != null) Deselect();
            _selectedVillager = villager;
            _selectedVillager.PauseWork(); // Metodo da aggiungere in VillagerController
            
            spotlight.gameObject.SetActive(true);
            spotlight.transform.SetParent(villager.transform);
            spotlight.transform.localPosition = Vector3.up * 5f;
            
            // Trigger UI (Task 3)
        }

        public void Deselect() {
            if (_selectedVillager != null) _selectedVillager.ResumeWork();
            _selectedVillager = null;
            spotlight.gameObject.SetActive(false);
            spotlight.transform.SetParent(this.transform);
        }
    }
}
```

- [ ] **Step 2: Collegare TouchInputSystem a DivineSelectionSystem**
Invece di loggare e basta, chiama `SelectVillager`.

- [ ] **Step 3: Test manuale**
Cliccare su un villager e verificare che si fermi e appaia la luce sopra di lui.

---

### Task 3: UI Overhead e Pensieri (Loyalty)

**Files:**
- Create: `Assets/_Project/Scripts/UI/OverheadMenuUI.cs`
- Modify: `Assets/_Project/Scripts/Village/VillagerController.cs`

- [ ] **Step 1: Implementare logica dei pensieri in VillagerController**
Aggiungere metodo per ottenere un pensiero basato sulla lealtà.

```csharp
public string GetCurrentThought() {
    if (loyalty > 80f) return "Sia lode a Te, Onnipotente!";
    if (loyalty < 30f) return "Ancora tu? Lasciami lavorare...";
    return "Ti ascolto, Signore.";
}
```

- [ ] **Step 2: Creare OverheadMenuUI**
Canvas World Space che segue il villager e mostra icone + testo.

- [ ] **Step 3: Test finale**
Selezionare villager con diversa Loyalty e verificare che il testo cambi correttamente.
