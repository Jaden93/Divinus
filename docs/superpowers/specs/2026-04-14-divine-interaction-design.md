# Spec di Design: Interazione Divina (Tap & Drag)

**Data**: 2026-04-14  
**Stato**: Approvato  
**Feature**: Selezione Divina (Tap) e Lancio Fisico (Drag & Throw)

## 1. Visione
Il giocatore interagisce con il mondo in due modi distinti: un tocco rapido per "parlare" o comandare (influenza sociale/informativa) e un trascinamento fisico per "spostare" la materia (potere fisico).

## 2. Selezione Divina (Tap)
### Comportamento
- Un singolo tap su un villager o un oggetto attivabile innesca la **Selezione**.
- Il target si ferma immediatamente (se villager, entra in uno stato di attesa/preghiera).
- Appare uno **Spotlight** (luce divina) dall'alto che illumina il target.

### Interfaccia (Overhead Icons)
- Tre icone circolari appaiono sopra la testa del villager (Canvas World Space):
  - **Info (ℹ️)**: Mostra statistiche (Salute, Energia, Lealtà, Tratti).
  - **Potere (⚡)**: Attiva un'azione specifica (es. Smite mirato o Benedizione).
  - **Chiudi (✖️)**: Deseleziona il target.
- Appare una **Speech Bubble** con un pensiero basato sulla `Loyalty` e sulla `Personality`.

## 3. Lancio Fisico (Drag & Throw)
### Il Sollevamento (Hold)
- La pressione prolungata (Long Press) "solleva" l'oggetto a un'altezza fissa (2.5m).
- **Ghost Mode**: Durante il sollevamento, le collisioni con l'ambiente (alberi, case) sono disabilitate per evitare intoppi.

### Il Rilascio (Release)
- **Deposito (Low Velocity)**: Se la velocità di trascinamento è bassa, l'oggetto viene posato gentilmente con snap al NavMesh.
- **Lancio (High Velocity)**:
  - Viene calcolata la velocità vettoriale al rilascio.
  - Si attiva il `Rigidbody` (non-kinematic).
  - Viene applicata una forza impulsiva.
  - Il villager entra in uno stato di **Ragdoll** o animazione di volo.
  - Una scia (VFX Trail) segue il villager lanciato.

### Collisioni Post-Lancio
- **Impatto Ambiente**: Se colpisce un albero, il villager subisce danni. L'albero riceve un impulso fisico (vibra o cade).
- **Impatto Sociale**: Se colpisce un altro villager, entrambi subiscono danni e perdono lealtà verso il Dio.
- **Impatto Terreno**: Danni da caduta basati sulla velocità verticale all'impatto.

## 4. Architettura Tecnica
### Classi Coinvolte
- `GodHand.cs`: Gestirà il calcolo della velocità di rilascio e l'impulso fisico.
- `DivineSelectionSystem.cs` (Nuovo): Gestirà il Tap, lo Spotlight e il menu overhead.
- `VillagerController.cs`: Aggiunta dello stato `DivineProjectile` e logica `OnCollisionEnter`.

## 5. Criteri di Accettazione (Checklist Unity)
- [ ] Il tap ferma il villager e mostra la luce.
- [ ] Le icone sopra la testa sono cliccabili.
- [ ] Il long press solleva il villager senza incastrarsi nelle case.
- [ ] Rilasciando velocemente, il villager vola via fisicamente.
- [ ] L'impatto con un albero danneggia il villager.
