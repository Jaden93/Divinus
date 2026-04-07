# CLAUDE.md — Progetto Unity Android ispirato a Black & White

## Visione

Realizzare un prototipo Android 3D in Unity in cui il giocatore è un dio che crea persone con il mana, modella un piccolo villaggio e può intervenire direttamente sul mondo (alberi, edifici, persone), in stile god game ispirato a *Black & White* ma con ritmo da idle/city builder.

Gli abitanti non sanno di essere NPC:
- all’inizio vagano senza fare nulla di produttivo;
- solo quando il dio li “indirizza” (es. donando un’ascia, costruendo una casa, posando una panca) iniziano a svolgere lavori e a vivere una routine;
- il dio può essere benevolo o sadico, spingendoli oltre lo sfinimento o proteggendoli.

## Obiettivo MVP

Consegnare una vertical slice mobile in cui:

- esiste una mappa piccola (isola con alberi);
- il dio può usare il mana per creare il primo umano;
- il lavoratore può ricevere un’ascia e tagliare alberi automaticamente per produrre legna;
- con la legna si possono costruire almeno:
  - una casa di legno dove il villager può dormire e recuperare energia;
  - una panca dove può riposare brevemente;
- il villager consuma energia lavorando, si stanca e:
  - usa panca o casa per riposare, poi torna a lavorare.

L’MVP deve dimostrare chiaramente:
- la relazione dio → persona → lavoro → costruzioni → qualità della vita;
- che il giocatore ha libertà di intervento (può creare, migliorare o distruggere).

## Piattaforma e stack
- Motore: Unity.
- Target primario: Android.
- Controllo primario: touch screen.
- Input secondario per sviluppo: mouse su editor Unity.
- Asset iniziali: file Blender della casa e del popolano.[cite:24][cite:27]

Unity supporta l'importazione di asset creati in Blender nel progetto e offre una pipeline pratica per prototipi Android rapidi, soprattutto quando si parte già da modelli `.blend` o da export controllati come FBX/glTF.[cite:24][cite:27]

## Regole generali per Claude Code
Claude Code deve lavorare sempre con task piccoli, verificabili e reversibili. Le modifiche devono essere limitate alla feature richiesta, evitando refactor inutili o sistemi futuri non ancora richiesti.[cite:19][cite:26]

Regole operative:
- Prima di ogni implementazione, proporre un piano breve in 3-7 step.[cite:19][cite:26]
- Modificare solo i file necessari alla task.
- Non introdurre nuove dipendenze senza richiesta esplicita.
- Non creare architetture "enterprise" per un MVP piccolo.
- Preferire codice chiaro, modulare e commentato solo dove davvero utile.
- Ogni task deve terminare con checklist di verifica manuale in Unity.
- Se una richiesta è troppo grande, dividerla in sotto-task prima di implementare.[cite:26][cite:32]

## Principi di design del gioco
Il prototipo deve rispettare questi principi:
- Influenza indiretta: il giocatore agisce sul mondo, non controlla un personaggio tradizionale.[cite:1]
- Reattività del villaggio: gli NPC devono reagire in modo visibile alle azioni divine.[cite:1][cite:7]
- Leggibilità mobile: pochi sistemi, chiari e visibili in sessioni brevi.[cite:7]
- Morale visibile: azioni benevole o oppressive devono cambiare reazioni, VFX, UI o stato degli NPC.[cite:1][cite:7]
- Scope ristretto: prima un loop di 60-90 secondi, poi eventuale espansione.[cite:18]

## Cose da NON fare nel MVP
- Nessuna creatura gigante nella prima milestone.
- Nessun combattimento complesso.
- Nessun sistema economico avanzato.
- Nessuna simulazione completa di più villaggi.
- Nessun multiplayer.
- Nessuna progressione meta complessa.
- Nessuna UI densa con tanti pulsanti o finestre.

## Loop di gioco iniziale
Loop MVP:
1. Il giocatore osserva il villaggio.
2. Tocca un'area o un abitante per interagire.
3. Usa un potere divino semplice.
4. Il popolano reagisce visivamente.
5. La `fede` del villaggio sale o scende.
6. Raggiunta la soglia obiettivo, il livello è completato.[cite:1]

## Struttura scene e sistemi
### Scene iniziali
- `Bootstrap`
- `VillagePrototype`
- `UISandbox`

### Sistemi minimi
- `TouchInputSystem`
- `VillageFaithSystem`
- `VillagerBehaviourSystem`
- `DivineActionSystem`
- `GameStateSystem`
- `SimpleAudioFeedback`

## Convenzioni codice
- Namespace principale: `DivinePrototype`.
- Un file C# per classe pubblica.
- Evitare singleton globali se non strettamente necessari.
- Preferire ScriptableObject solo quando semplifica davvero dati ripetuti.
- Nomi chiari: `VillagerController`, `FaithMeterUI`, `DivineBlessingAction`.
- Tutto il codice nuovo deve essere orientato al prototipo, non a una versione finale ipotetica.

## Convenzioni asset
Asset Blender esistenti:
- casa
- popolano

Regole asset:
- Naming coerente in inglese, snake_case o lowerCamel solo se già standardizzato.
- Pivot corretti prima dell'import.
- Scala coerente tra asset.[cite:24][cite:27]
- Materiali semplici e leggeri per mobile.
- Mesh low poly o comunque controllate.
- Collisioni semplici, preferibilmente primitive quando possibile.
- Animazioni del popolano essenziali: idle, walk, reaction_pray, reaction_happy, reaction_fear.

## Cartelle consigliate
- `Assets/_Project/Scripts/Core`
- `Assets/_Project/Scripts/Input`
- `Assets/_Project/Scripts/Village`
- `Assets/_Project/Scripts/Divine`
- `Assets/_Project/Scripts/UI`
- `Assets/_Project/Art/Models`
- `Assets/_Project/Art/Materials`
- `Assets/_Project/Prefabs`
- `Assets/_Project/Scenes`
- `Assets/_Project/Audio`

## Prima roadmap MVP — 2 settimane

### Settimana 1
#### Task 1 — Setup progetto
Obiettivo:
- Creare progetto Unity Android.
- Impostare cartelle base.
- Preparare scena `VillagePrototype`.
- Configurare camera isometrica o semi-top-down adatta al touch.

Definition of done:
- Il progetto builda in editor senza errori.
- Esiste una scena giocabile vuota con terreno e camera.

#### Task 2 — Import asset Blender
Obiettivo:
- Importare casa e popolano.
- Verificare scala, pivot, materiali e collider.
- Creare prefab base per casa e popolano.[cite:24][cite:27]

Definition of done:
- La casa è visibile e posizionata correttamente sul terreno.
- Il popolano è visibile, con orientamento corretto.
- I prefab sono riutilizzabili.

#### Task 3 — Movimento base NPC
Obiettivo:
- Dare al popolano un comportamento semplice: idle e camminata verso waypoint.
- Aggiungere stati minimi leggibili.

Definition of done:
- Il popolano alterna idle e walk.
- Il comportamento è stabile e non glitcha.

#### Task 4 — Touch input divino
Obiettivo:
- Implementare tap e drag basilari.
- Consentire la selezione di un punto del terreno o di un NPC.
- Aggiungere un feedback visivo semplice sul tocco.

Definition of done:
- Da editor con mouse e su touch il click/tap è rilevato.
- Il feedback è visibile e coerente.

### Settimana 2
#### Task 5 — Fede del villaggio
Obiettivo:
- Introdurre variabile `faith` globale del villaggio.
- Creare una semplice barra UI.
- Collegare le azioni divine a variazioni della fede.

Definition of done:
- La barra si aggiorna in tempo reale.
- Il valore è clampato e leggibile.

<!-- SKIPPED — Task 6 — Azione divina primaria
Motivo: rimandato dopo MVP. Il loop base (NPC + fede + vittoria) deve essere stabile prima di aggiungere azioni divine esplicite.
Obiettivo originale:
- Implementare una sola azione, ad esempio `BlessVillage`.
- L'azione deve attivarsi con tap su area valida.
- L'effetto deve avere feedback visivo e sonoro minimo.
-->

#### Task 7 — Reazione del popolano
Obiettivo:
- Far reagire l'NPC all'azione divina.
- Reazioni minime: happy o pray, eventualmente fear in caso di azione negativa.[cite:1][cite:7]

Definition of done:
- L'NPC cambia stato in base all'evento.
- Il feedback è leggibile anche senza debug UI.

#### Task 8 — Condizione di vittoria
Obiettivo:
- Chiudere il loop MVP con obiettivo di fede al 100%.
- Mostrare schermata semplice di successo.

Definition of done:
- La partita ha inizio, progresso e fine.
- Il ciclo completo dura circa 60-90 secondi.

## Prompt base per Claude Code

### Prompt 1 — Pianificazione iniziale
```text
Leggi questo file CLAUDE.md e proponi un piano di implementazione in massimo 6 step per creare il prototipo Unity Android descritto. Non scrivere ancora codice. Evidenzia i file C# che pensi di creare, le scene coinvolte e i rischi tecnici principali. Mantieni lo scope strettamente MVP.
```

### Prompt 2 — Setup progetto
```text
Leggi CLAUDE.md. Implementa solo il setup iniziale del prototipo Unity:
- cartelle base in Assets/_Project
- scena VillagePrototype
- terreno semplice
- camera semi-top-down adatta al touch
Non aggiungere gameplay. Prima mostrami un piano breve, poi applica le modifiche. Alla fine dammi checklist manuale di verifica in editor.
```

### Prompt 3 — Import prefab
```text
Leggi CLAUDE.md. Prepara il progetto per usare i due asset Blender già esistenti: casa e popolano. Crea prefab, assegna collider semplici, verifica scala e orientamento. Non implementare ancora AI o UI. Prima proponi un piano breve e limita le modifiche ai file strettamente necessari.
```

### Prompt 4 — NPC base
```text
Leggi CLAUDE.md. Implementa il comportamento base del popolano con stati idle e walk tra waypoint. Usa codice semplice e modulare. Non introdurre sistemi complessi di behaviour tree. A fine task aggiungi checklist di test manuale.
```

### Prompt 5 — Touch input
```text
Leggi CLAUDE.md. Implementa un sistema touch/mouse minimale per selezionare terreno o NPC nella scena VillagePrototype. Aggiungi feedback visivo semplice sul punto toccato. Non creare ancora poteri multipli.
```

### Prompt 6 — Fede e azione divina
```text
Leggi CLAUDE.md. Implementa una variabile globale di fede del villaggio, una barra UI minimale e una sola azione divina chiamata BlessVillage che aumenta la fede quando il giocatore tocca una zona valida. Collega una reazione semplice del popolano. Mantieni il codice piccolo e leggibile.
```

## Regole di revisione
Ogni pull o modifica generata da Claude Code deve essere controllata con queste domande:
- Questa feature serve davvero all'MVP attuale?
- Il codice è il minimo necessario?
- È testabile manualmente in meno di 2 minuti?
- Introduce complessità futura non richiesta?
- Mantiene il feeling di influenza indiretta tipico del riferimento originale?[cite:1]

### Nota per step successivi

Dopo l’MVP 0, le prossime feature prioritarie sono:

- **Case multiple**
  - Permettere di costruire più case di legno.
  - Ogni casa offre almeno 1 posto letto per un villager.
  - Non ci sono limiti fissi al numero di case, finché ci sono risorse.

- **Punti di riposo leggeri (es. panca)**
  - Nuovo tipo di costruzione a basso costo (prima panca).
  - I villager stanchi possono usarla per recuperare un po’ di energia senza tornare a casa.

Questi sistemi devono essere implementati in step separati, solo dopo che il loop base uomo → ascia → legna → casa → sonno è stabile.

## Espansione dopo MVP

Solo dopo che l’MVP 0 (uomo → ascia → legna → prima casa) è stabile e divertente, le feature candidate sono:

- Ruolo **minatore**:
  - nuovi nodi di risorsa (pietra);
  - uso della pietra per case Tier 2 e edifici avanzati.

- **Casa di legno Tier 2** (legno + pietra):
  - upgrade delle case esistenti;
  - riposo migliore e possibili bonus alla produttività.

- **Menu delle creazioni**:
  - tab Strumenti (es. AXE) da trascinare sui villager;
  - tab Costruzioni (case, depositi, punti di riposo) con costi in risorse e timer.

- Punti di riposo leggeri:
  - piccole costruzioni (panchine, fuochi) per recuperare un po’ di stanchezza senza dormire.

- Prime decorazioni estetiche:
  - elementi che non cambiano molto il power, ma rendono il villaggio più bello da osservare.

La monetizzazione (timer accelerabili, lavoratori aggiuntivi) dovrà appoggiarsi a questi sistemi, non sostituirli, e verrà progettata solo dopo che il loop di base e il ritmo giornaliero saranno chiari.