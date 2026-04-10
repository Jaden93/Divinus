# Mining & Generic Resources System - Progress Handover

## Stato Corrente (Sessione Pomeridiana)
Abbiamo completato l'unificazione del sistema di risorse e l'implementazione del mining. Il villaggio ora usa un'infrastruttura scalabile e pronta per il trasporto fisico avanzato.

### Core Systems Implementati:
1.  **`ResourceManager.cs`**: Il nuovo "cervello" delle risorse. Gestisce Legna e Pietra in un unico posto.
    *   Supporta cap differenziati (con/senza deposito).
    *   Sistema di eventi unificato per la UI.
2.  **`GenericDepotController.cs`**: Uno script per l'edificio "Deposito". I villager portano sia legna che pietra qui. Quando questo edificio è presente in scena, i cap del villaggio aumentano automaticamente.
3.  **`StoneNode.cs` & `StoneManager.cs`**: Logica per le rocce minerarie. Il manager assegna i compiti ai villager con il piccone.
4.  **`Pickaxe System`**:
    *   `PickaxePickup.cs`: Gestisce il piccone a terra.
    *   `PickaxeActionUI.cs`: Gestisce il rilascio del piccone dal menu divino.
5.  **`VillagerController.cs` (Aggiornato)**: 
    *   Ora supporta gli stati `Mining` e `CarryingStone`.
    *   Cerca il `GenericDepotController` più vicino per consegnare qualsiasi risorsa.
    *   Se non c'è un deposito fisico, consegna istantaneamente (fallback per inizio gioco).

### Cap Risorse Attuali:
- **Legna**: 9 (iniziale) -> 30 (con deposito).
- **Pietra**: 3 (iniziale) -> 20 (con deposito).

---

## Task Suggeriti per Stasera:

### 1. Configurazione Scena Unity (Manuale)
- Creare un nuovo Prefab chiamato **GenericDepot** usando un modello di magazzino/tettoia.
- Aggiungere il componente `GenericDepotController` a questo prefab.
- Aggiornare il sistema di costruzione (ConstructionMenu) affinché il giocatore possa posizionare questo deposito generico.

### 2. Integrazione UI
- Aggiornare la barra delle risorse in alto per mostrare anche l'icona della Pietra.
- Collegare `ResourceManager.Instance.stone.onChanged` al nuovo testo della UI.

### 3. Effetti Visivi & Animazioni
- Nel prefab del Villager, assicurarsi che l'Animator abbia il trigger `isMining`.
- Aggiungere un effetto particellare di "scintille" quando il villager colpisce la roccia.

### 4. Gameplay Loop: Costruzione con Pietra
- Modificare i prefab delle Case o della Chiesa affinché richiedano sia Legna che Pietra per essere completati.
- Aggiornare `ConstructionSite.cs` per consumare risorse dal `ResourceManager`.

### 5. Futuro: Asini e Carretti
- Il `GenericDepotController` ha già il metodo `GetDeliveryPosition()`. Si può estendere per gestire una "coda" di carretti o animali da soma che scaricano grandi quantità di risorse.

---
*L'infrastruttura è solida e disaccoppiata dal lavoro dell'altro agent sulla coscienza dei villager. Buono sviluppo!*
