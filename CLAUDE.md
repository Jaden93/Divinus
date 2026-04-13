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

## Stato del Progetto (Aprile 2026)

### Feature Implementate e Integrate
- **Risorse Centralizzate (`ResourceManager.cs`)**: Gestione di Legna e Pietra con cap dinamici.
- **Loop Risorse Completo**:
  - **Taglialegna**: Taglio alberi per legna.
  - **Minatore**: Estrazione pietra da nodi rocciosi.
  - **Trasporto**: I villager portano le risorse al deposito.
- **Costruzioni**:
  - **Casa (Tier 1)**: Costa 6 Legna e 3 Pietra. Navigazione NavMesh funzionante (entrata/uscita).
  - **Deposito Generico**: Aumenta i cap delle risorse (Legna: 30, Pietra: 20).
- **Sistemi Divini**:
  - **Smite**: Fulmine che colpisce oggetti (danneggiandoli) o villager.
  - **Morte**: I villager possono morire per Smite, sfinimento (Energia = 0), morte ( vita = 0 ) .
- **UI & Feedback**:
  - **DebugHUD**: Visualizzazione real-time di Legna, Pietra e Fede.
  - **ObjectiveHintUI**: Guida contestuale per il giocatore (es. "Raccogli 6 legna e 3 pietra").

### Sistemi Core
- `TouchInputSystem`: Selezione e interazione touch/mouse.
- `VillageFaithSystem`: Fede come unica risorsa divina (sostituisce il Mana).
- `VillagerController`: Macchina a stati (Idle, Work, Sleep, Dead, Mining).
- `ConstructionSite`: Gestione cantieri con costi e timer.

## Roadmap Sprint 1: "La Mano e il Cuore" (In Corso)
*Focus: Interazione diretta, sopravvivenza e feedback sociale immediato.*

1. **Animator Morte & Revive** (ID 1-2):
   - Setup trigger animator `isDead`.
   - Logica potere `Revive` (costo 15 Fede, ripristino 50% energia).
2. **Drag & Drop Totale & Ruota Contestuale** (ID 16-3):
   - Possibilità di sollevare e spostare fisicamente Villager e Oggetti.
   - Menu radiale per interazioni: *Sposta*, *Dettagli*, *Punisci*, *Estetica*.
3. **Smite su Risorse & Logica Depot** (ID 17-18):
   - Smite su alberi/rocce distrugge il nodo e genera cubi risorsa raccoglibili.
   - Start con 5 legna; tutorial per il posizionamento obbligatorio del Deposito.
4. **Sistema Strade & Navigazione** (ID 19):
   - Implementazione strade con costo NavMesh ridotto (preferenza di percorso).
   - *Strategia Collisioni*: Uso di `ObstacleAvoidanceType.HighQuality` e pesi variabili per evitare ingorghi sui path stretti.
5. **Malattia & Ciclo Giorno/Notte** (ID 9-5):
   - Stato `Sickness` (Pioggia) e necessità di riposo notturno sincronizzato.
   - Arredamento obbligatorio (**Letto, Luce, Armadio**) per rendere le case abitabili.
6. **Allineamento Visuale & Pensieri** (ID 10-11):
   - VFX Passi Divini (Fiori vs Cenere) e Speech Bubbles (🤒, 🌩️, ❤️).

## Roadmap Sprint 2: "Entità e Trasformazione" (Pianificato)
*Focus: Unità speciali, biomi e evoluzione del mondo.*

1. **Il Missionario (ID 12)**: Unità IA benevola che cura e benedice.
2. **Terraforming Avanzato & Mestieri**: Cambio reale dei biomi e introduzione di *Minatore* e *Agricoltore*.
3. **Creatura Divina (Avatar)**: Introduzione del compagno animale.
4. **Evoluzione Architettonica**: Case che cambiano stile visivo in base alla moralità.

## Visione a Lungo Termine (Post-Sprint)
- **Mondo Sferico**: Rendering dell'isola su superficie curva (Vertex Shader).
- **Cataclismi**: Poteri di distruzione su larga scala (Tornado, Terremoti).
- **Multi-Villaggio**: Simulazione di comunità diverse in conflitto o alleanza.



---

## Design Document — Feature Future (post-MVP)

Questa sezione raccoglie le decisioni di design prese per le feature pianificate dopo l'MVP. Non implementare nulla qui finché il loop base non è stabile.

---

### Sistema Fede — Rework

Il **mana non esiste**. La fede è l'unica risorsa del giocatore.

- La fede è **globale** del villaggio (un solo valore, `VillageFaithSystem`).
- Spendere fede per creare persone, creature o usare poteri **riduce** il valore globale.
- La fede cresce o rallenta in base allo stato emotivo degli abitanti:
  - Abitanti **felici** → fede aumenta velocemente.
  - Abitanti **tristi** → fede aumenta lentamente.
  - Abitanti **spaventati** → fede non aumenta.
- Soglie di fede sbloccano poteri più potenti e riducono i timer di costruzione.

---

### Fede Individuale dei Popolani

Ogni `VillagerController` avrà un attributo `individualFaith` (float 0–1) che rappresenta il livello di devozione personale. Categorie indicative:

| Livello | Etichetta | Comportamento |
|---|---|---|
| 0.8–1.0 | Fanatico / Uomo di chiesa | Bonus produttività, reagisce positivamente a interventi divini |
| 0.5–0.8 | Credente normale | Reazioni miste casuali agli interventi, possibili commenti positivi/negativi |
| 0.2–0.5 | Tiepido / Scettico | Interventi divini aumentano lo scetticismo |
| 0.0–0.2 | Eretico / Non fedele | Cospira attivamente: cerca di convincere chi ha fede debole a ribellarsi |

**Effetti collaterali degli eretici:**
- Possono causare **attacchi vandalici** a edifici o risorse.
- Possono "contagiare" vicini con fede bassa, innescando rivolte.
- Vanno tenuti sotto controllo o convertiti con il potere Conversione.

**Effetto drag su un villager:**
- Fanatico → lo interpreta come messaggio divino (effetto positivo).
- Credente normale → reazione casuale con commento positivo o negativo (influenza fede globale di poco).
- Scettico → aumenta il suo scetticismo.
- Eretico → reazione di paura/fuga.

---

### Creature Benevolenti e Malefiche

Il giocatore può evocare creature spendendo fede. Più creature possono coesistere (sandbox).

**Creatura Benevola:**
- Aspetto piccolo, piacevole, non minaccioso.
- Effetti passivi: leggero bonus al morale/produttività dei villager nelle vicinanze.
- Non genera paura.
- **Condizione speciale:** i villager nelle vicinanze **recuperano più velocemente ferite e infortuni**.

**Creatura Malefica:**
- Aspetto imponente e pauroso, occupa spazio fisico nella mappa.
- Effetti passivi: bonus generale alla produttività (paura come motivatore).
- Aumenta progressivamente il livello di paura degli abitanti nelle vicinanze.
- Se la paura supera una soglia critica → i villager colpiti **abbandonano permanentemente** il villaggio.
- **Condizione speciale:** i villager nelle vicinanze **non vanno a dormire di notte** — lavorano senza sosta ma si esauriscono più rapidamente.
- Bilanciamento chiave: bonus forti ma rischio reale di perdere abitanti. Il giocatore sceglie consapevolmente.

**Note implementative future:**
- Classe base `CreatureController` con tipo enum (Benevolent/Malevolent).
- Effetti via trigger di prossimità (sfera collider).
- AI minima: presenza statica o pattugliamento semplice.

---

### Sistema Drag & Drop Globale — Ruota Contestuale

Il giocatore attiva la modalità interazione tenendo premuto su qualsiasi oggetto o persona nel mondo. Appare una **ruota contestuale** con voci variabili per tipo di target.

**Voci comuni (tutti i target):**
- **Sposta** — trascina l'elemento in una nuova posizione.
- **Dettagli** — mostra informazioni sull'oggetto/persona.
- **Estetica** — personalizza aspetto (decorazioni, colori, accessori — solo visuale).
- **Potenzia** — upgrade funzionale (costo risorse o fede, implementato in futuro).

**Voci specifiche per tipo:**
- *Villager*: Sposta / Dettagli / Estetica / Potenzia
- *Casa / Edificio*: Sposta / Dettagli / Estetica / Potenzia / Distruggi
- *Albero / Nodo risorsa*: **Sradica** (ottieni legna/pietra/ecc. immediatamente, nessuno spostamento)
- *Creatura*: Sposta / Dettagli / Potenzia

**Regola fondamentale:** il giocatore non parla direttamente agli abitanti. È Dio. Ogni interazione avviene tramite azione fisica sul mondo, non tramite dialogo.

**Note implementative future:**
- `RadialMenuController.cs` — genera la ruota in base al tipo di `IInteractable` toccato.
- Distinzione tra long-press (ruota) e tap rapido (selezione/feedback).
- Compatibile con touch e mouse.

---

### Poteri Divini — Catalogo

I poteri costano fede e hanno un **cooldown**. Sbloccati per soglia di fede.

| Potere | Effetto | Complessità stimata |
|---|---|---|
| **Cambio Giorno/Notte** | Forza la transizione immediata da giorno a notte o viceversa. | Media |
| **Cambio Meteo** | Alterna sole/pioggia. Pioggia rallenta lavori, sole li accelera. | Media |
| **Revive** | Resuscita un villager o animale morto. | Media |
| **Costruzione Immediata** | Completa istantaneamente un cantiere in corso. | Bassa |
| **Evoca Creatura** | Crea una creatura benevola o malefica. | Alta |
| **Cataclisma** | Distrugge tutto in un'area piccola (circa dimensione di una casa). | Media |
| **Conversione** | Trasforma un eretico/vandalo in credente. | Bassa |
| **Ispirazione** | Tocca un villager: lavora più veloce e con morale alto per un periodo. | Bassa |

---

### Ruoli Lavoratori Futuri

Dopo il boscaiolo (già implementato), i ruoli pianificati sono:

- **Pescatore** — nodi acqua, produce cibo.
- **Minatore** — nodi pietra, sblocca costruzioni Tier 2.
- **Agricoltore / Coltivatore** — campi coltivabili, produce cibo.
- **Allevatore** — animali domestici, produce risorse secondarie.

Ogni ruolo richiede uno strumento specifico assegnabile tramite drag & drop dal pannello UI (come l'ascia attuale).

---

### Ciclo Giorno / Notte

Il mondo ha un ciclo giorno/notte continuo che influenza i comportamenti dei villager e l'atmosfera visiva.

**Comportamenti:**
- **Giorno** — villager lavorano normalmente.
- **Notte** — tutti i villager dormono. Chi ha casa torna al proprio letto. Chi non ha casa si sdraia sul posto dove si trova.

**Potere divino — Cambio Giorno/Notte:**
- Il giocatore può forzare la transizione in qualsiasi momento spendendo fede.
- Utile per far riposare i villager prima che si esauriscano, o per avviare subito un nuovo giorno lavorativo.
- Ha cooldown per evitare abuso.

**Note implementative future:**
- `DayNightCycle.cs` — gestisce timer, rotazione luce direzionale, skybox blend.
- `Shader.SetGlobalFloat("_TimeOfDay", ...)` per effetti globali su shader.
- I villager reagiscono all'evento `OnNightBegins` / `OnDayBegins`.

---

### Decorazioni

Puramente estetiche, nessun effetto gameplay. Applicabili tramite voce "Estetica" della ruota contestuale su villager o edifici. Progettate dopo che il loop economico è stabile.