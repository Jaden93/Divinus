# Mining & Generic Resources System - Progress Handover

## Stato Corrente (Sessione Serale - Completata Integrazione)
Abbiamo completato l'integrazione del mining nel loop di gioco e migrato tutti i sistemi UI/Costruzione al nuovo `ResourceManager`.

### Core Systems Implementati & Integrati:
1.  **`ResourceManager.cs` (CENTRALIZZATO)**: Sostituisce i vecchi sistemi di deposito.
    *   Gestisce Legna e Pietra con cap dinamici (9/3 iniziali, 30/20 con deposito).
    *   Fornisce l'API `HasResources(wood, stone)` e `SpendResource(type, amount)`.
    *   Sostituisce `GameStateSystem.WoodCount` e `WoodDepot.woodCount`.
2.  **`ConstructionSite.cs` & `HouseActionUI.cs`**:
    *   La Casa ora costa **6 Legna e 3 Pietra**.
    *   Il pulsante si abilita solo con entrambe le risorse disponibili.
    *   Il consumo è gestito via `ResourceManager`.
3.  **`DebugHUD.cs`**:
    *   Ora mostra sia **WOOD** che **STONE** in tempo reale leggendo dal manager.
4.  **`ObjectiveHintUI.cs`**:
    *   Loop obiettivi aggiornato: "Collect wood and stone (6 wood, 3 stone)".
    *   Ascolta gli eventi di `ResourceManager` per avanzare.
5.  **`VillagerController.cs`**:
    *   Logica di mining e trasporto pietra già integrata con l'Animator (`isMining`).

### Cap Risorse:
- **Legna**: 9 (base) -> 30 (con `GenericDepotController` in scena).
- **Pietra**: 3 (base) -> 20 (con `GenericDepotController` in scena).

---

## Operazioni Manuali Rimaste (In Editor Unity):

1.  **Generic Depot Prefab**:
    *   Creare un prefab per il deposito (es. tettoia) e aggiungervi `GenericDepotController`.
    *   Assegnare questo prefab al campo `depotPrefab` nel componente `WoodDepotActionUI` (che ora funge da posizionatore di depositi generici).
2.  **UI Grafica (Opzionale)**:
    *   Se desiderato, aggiungere un'icona Pietra permanente nella UI principale (il `DebugHUD` la mostra già testualmente).
3.  **VFX**:
    *   Aggiungere particelle di scintille nel punto di contatto durante l'animazione di mining.

---
*L'integrazione tecnica è terminata. Il loop Legno + Pietra -> Casa è ora il nuovo standard del villaggio.*
