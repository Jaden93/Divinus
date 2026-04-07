# GAME_DESIGN.md — Prototipo God/Idle Android ispirato a Black & White

## Alta visione (aggiornata)

Il gioco è un **god-sim idle city builder**: il giocatore è un dio che crea persone e modella un villaggio/città, ma gli abitanti non sanno di essere NPC.

Punti chiave della fantasia:

- Il dio usa **mana** per creare nuove persone.
- Le persone appena create vagano confuse, senza sapere cosa fare.
- Solo quando il dio interviene (es. donando un’ascia, assegnando un ruolo, costruendo una casa) la loro vita prende una direzione.
- Il dio ha **pieno libero arbitrio**:
  - può sradicare alberi e scuoterli per ottenere legna;
  - può distruggere edifici e oggetti;
  - può ferire o uccidere persone;
  - può salvare, curare, fermare risse, riportare l’ordine.

Gli NPC vivono come se fossero “reali”; nel tempo, se la comunità cresce e prospera, iniziano a:
- chiedere al dio nuove costruzioni o miracoli (es. chiesa, infermeria, difese);
- sviluppare desideri collettivi (più case, più sicurezza, più comfort);
- (in futuro) mettere in discussione la propria esistenza, fino a poter scoprire che vivono in una simulazione se il dio usa il “potere della verità”.


## Stili di gioco: dio buono vs dio sadico

Il gioco è single-player e dà al giocatore libertà totale su come trattare il villaggio.

Due poli principali:

- **Dio buono**
  - Usa la fede per creare persone e aiutarle.
  - Garantisce strumenti, case e punti di riposo.
  - Non forza i lavoratori oltre la stanchezza, cerca il loro benessere.
  - Costruisce una comunità felice e ordinata, che chiede nuove strutture “positive” (chiesa, infermeria, luoghi di ritrovo).

- **Dio sadico**
  - Può costringere i lavoratori a lavorare fino allo sfinimento.
  - Può ignorare il loro bisogno di riposo.
  - Può distruggere edifici e uccidere persone, fino a spazzare via il villaggio e costringersi a ricominciare da zero.
  - Usa il villaggio più come “giocattolo” che come comunità da proteggere.

Il mondo reagisce nel tempo alle scelte del giocatore:
- un villaggio ben gestito cresce, chiede strutture più complesse e sviluppa richieste sempre più sofisticate verso il dio;
- un villaggio abusato può diventare caotico, spaventato o collassare completamente.

## Potere della verità (idea futura)

In una fase avanzata, il dio può usare un “potere della verità” per rivelare ad alcuni abitanti che:
- non sono reali nel senso tradizionale;
- vivono in un mondo controllato da una volontà esterna (il giocatore).

Possibili conseguenze (solo design, non per l’MVP):
- alcuni abitanti potrebbero diventare ancora più devoti;
- altri potrebbero reagire con rifiuto o panico;
- la comunità potrebbe cambiare comportamento collettivo.

Questo sistema è pensato come obiettivo di lungo periodo, quando il villaggio è già complesso e stabile.


## Setup iniziale del mondo

### Stato iniziale
- Il giocatore avvia un **nuovo mondo**.
- La scena mostra un’isola relativamente piccola, piena di alberi e terreno naturale, ma **senza edifici né villaggio**.
- Non esistono ancora lavoratori o case.

### Intervento divino iniziale
- Il primo atto del giocatore è **creare il primo umano**.
- Questa creazione dell’uomo è un atto divino chiave che introduce:
  - il primo popolano (worker base);
  - il punto di partenza della civiltà.

## Comportamento dell’uomo (popolano)

### Azioni automatiche
- Il popolano ha azioni automatiche base (idle, piccoli spostamenti, osserva gli alberi), ma **non sa ancora lavorare in modo produttivo**.
- A volte si **rifiuta di procedere** o resta indeciso: il giocatore deve intervenire con il proprio potere per guidarlo.

### Intervento del giocatore
- Il dio può indicare un albero, “ispirare” il popolano o consegnargli un oggetto (l’ascia).
- Il feeling deve essere: l’uomo è semi-autonomo, ma la divinità gli mostra cosa fare.

## Risorsa e primi sistemi idle

### Risorsa: legna
- Il primo ciclo idle ruota attorno alla **legna**.
- Gli alberi sull’isola sono la fonte primaria di legno.

### Creazione dell’ascia
- All’inizio il popolano non può raccogliere legna a mani nude.
- Task chiave del tutorial: il giocatore deve creare/procurare una **ascia** per il popolano.
  - Azione divina: forgiare o donare un’ascia.
  - Effetto: il popolano può iniziare a tagliare alberi in modo automatizzato.

### Loop base legna
1. Il giocatore crea l’uomo.
2. Il giocatore lo aiuta a ottenere un’ascia.
3. Il popolano, ora con l’ascia, comincia a tagliare alberi automaticamente nel tempo.
4. Ogni albero tagliato genera **legna** che va nell’inventario del villaggio.
5. La legna è la risorsa necessaria per costruire la prima casa.

Questo ciclo è il primo vero **loop idle/incrementale**: una volta impostato, il popolano continua a produrre legna senza intervento costante.

## Costruzione della prima casa

### Obiettivo del primo slice (MVP 0)
Per il primo MVP giocabile, l’obiettivo massimo sarà:
- creare l’uomo;
- dargli l’ascia;
- raccogliere abbastanza legna;
- **costruire una casa**;
- vedere il popolano usare la casa per dormire/ripararsi.

### Meccanica
- Quando la legna raggiunge una soglia minima, si sblocca l’azione “Costruisci casa”.
- Il popolano (o i lavoratori disponibili) iniziano il lavoro di costruzione.
- La costruzione richiede **tempo reale** (timer):
  - può essere lasciata scorrere naturalmente;
  - in futuro potrà essere accelerata con poteri divini/valuta premium o lavoratori aggiuntivi.

### Uso della casa
- Una volta costruita, la casa diventa il punto in cui il popolano dorme e si riposa.
- Più avanti questo potrà influire su:
  - velocità di lavoro;
  - morale/fede;
  - crescita della popolazione.

## Fede, popolazione e bene/male (per fasi successive)
- La **fede** misura quanto il villaggio crede nel dio.
- Azioni di aiuto aumentano la fede, azioni crudeli o distruttive la riducono.
- L’aumento della popolazione avverrà attraverso la procreazione, rappresentata in gameplay dal potere divino di far comparire nuovi lavoratori quando ci sono abbastanza fede, casa e risorse.
- La componente “cattiva” permette al dio di distruggere alberi, strutture o popolani senza consenso, con vantaggi a breve termine ma conseguenze sul lungo periodo.

Per ora, tutte queste parti (fede avanzata, popolazione multipla, morale profonda) si considerano **fuori scope per l’MVP 0** e saranno aggiunte solo dopo che il ciclo uomo–ascia–legna–casa funziona bene.

## Mestieri dei villager (futuro)

Nel medio periodo i nostri villager potranno specializzarsi in diversi mestieri. Per l’MVP 0 è presente solo il taglialegna, ma il design prevede:

### Taglialegna
- Stato: già presente nel prototipo.
- Compito: tagliare alberi per generare **legna** in modo automatico.
- Uso della legna: costruzioni base (casa di legno, oggetti di riposo), upgrade futuri.
- Limite: si ferma quando il **deposito legna** raggiunge la capienza massima.

### Minatore
- Stato: solo design, non implementato in MVP 0.
- Compito: estrarre **pietra** da nodi rocciosi.
- Uso della pietra: upgrade delle case (da legno a legno+pietra), edifici più resistenti o avanzati.
- Note: verrà introdotto quando il loop legna → casa sarà stabile.

### Costruttore
- Stato: solo design, non implementato in MVP 0.
- Compito: usare legna e pietra per **costruire o migliorare edifici**.
- Non genera risorse, ma trasforma risorse in strutture (case, depositi, punti di riposo).
- Interagisce con cantieri e timer di costruzione.

### Altri mestieri possibili (solo idea)
- Contadino / raccoglitore cibo: mantiene alta l’energia/fame dei villager.
- Artigiano / decoratore: crea oggetti estetici (panchine, statue, lampioni) che rendono il mondo più bello.

Questi mestieri servono a dare profondità nel tempo, ma non devono essere sviluppati prima che il ciclo base uomo → ascia → legna → casa funzioni bene.


## Costruzioni base

Oltre agli alberi e al deposito di legna già presenti nel prototipo, il gioco ruoterà intorno a poche costruzioni principali, pensate per sessioni brevi ma soddisfacenti.

### Deposito legna
- Stato: già presente nel prototipo.
- Funzione: contiene la legna prodotta dal taglialegna.
- Limite: ha una **capienza massima**; quando è pieno, il taglialegna smette di lavorare.
- Design F2P: in futuro il deposito potrà essere migliorato per aumentare la capienza, oppure si potranno sbloccare nuovi depositi.

### Casa di legno (Tier 1)
- Stato: obiettivo immediato dopo l’MVP 0.
- Costi: solo legna.
- Funzione:
  - fornisce un posto dove il villager può **dormire** e recuperare stanchezza;
  - rende visibile il progresso del villaggio (prima “vera” casa).
- Integrazione con il loop:
  - costruita dopo aver raccolto abbastanza legna;
  - richiede tempo reale (timer) per essere completata.

### Casa migliorata legno + pietra (Tier 2)
- Stato: solo design, non implementata in MVP 0.
- Costi: legna + pietra.
- Funzione:
  - riposo più efficace (meno tempo per recuperare energia);
  - potenziale aumento della capacità di popolazione o bonus produttivi.
- Scopo: dare un senso agli upgrade e all’introduzione del minatore.

### Punti di riposo leggeri
- Stato: solo idea per fasi successive.
- Esempi: panche, fuochi da campo, piccoli rifugi aperti.
- Funzione:
  - permettono ai villager di recuperare una parte di stanchezza senza tornare a casa;
  - rendono il mondo più “vivo” e piacevole da osservare.

### Decorazioni e abbellimenti
- Stato: idea long-term.
- Non danno grandi bonus di potere, ma servono a:
  - personalizzare l’isola;
  - dare un obiettivo estetico ai giocatori che vogliono un’esperienza cozy.


  ## Loop giornaliero (gioco da pochi minuti al giorno)

Il gioco è pensato per essere giocato **pochi minuti al giorno**, non per lunghe sessioni continue.

Loop tipico:
1. Il giocatore apre il gioco e vede cosa hanno fatto i villager mentre era offline (legna raccolta, costruzioni avanzate, villager stanchi).
2. Raccoglie le risorse passive (legna, e in futuro pietra/cibo).
3. Usa le risorse per prendere 1–3 decisioni significative:
   - avviare o completare una nuova costruzione / upgrade;
   - sbloccare o riassegnare un lavoratore (nuovo mestiere);
   - posizionare nuovi oggetti di riposo o decorazioni.
4. Chiude il gioco, lasciando i villager a continuare a lavorare in background.

Questo ritmo supporta un’esperienza rilassata, dove il piacere principale è vedere il villaggio crescere e abbellirsi gradualmente.

## Menu delle creazioni

Oltre al menu degli strumenti (es. AXE da trascinare sul villager), il gioco avrà un **menu delle creazioni** che permette di spendere le risorse raccolte per costruire edifici e oggetti di riposo.

### Struttura generale

Il menu delle creazioni è pensato come un pannello semplice con due tab principali:

- **Tab Strumenti**
  - Contiene gli oggetti che il dio può “donare” ai villager (es. AXE).
  - Ogni strumento ha:
    - un costo in risorse (se previsto);
    - eventuali requisiti (es. avere sbloccato un certo edificio o progresso);
    - un comportamento di drag & drop (trascina sul villager per assegnarglielo).

- **Tab Costruzioni**
  - Contiene le strutture che possono essere piazzate nel mondo.
  - Ogni costruzione ha:
    - un costo in risorse (legna, in futuro pietra, ecc.);
    - un tempo di costruzione (timer);
    - un limite di quantità (es. massimo X case di legno all’inizio).

### Flusso di costruzione

1. Il giocatore apre il menu delle creazioni (tab Costruzioni).
2. Seleziona una costruzione disponibile (es. “Casa di legno”).
3. Vede i requisiti:
   - costo in risorse (es. 50 legna);
   - stato delle risorse correnti (es. 50/50).
4. Se i requisiti sono soddisfatti, può:
   - scegliere una posizione valida nel mondo;
   - confermare la costruzione.
5. Viene creato un **cantiere**:
   - parte un **timer di costruzione**;
   - uno o più villager (in base al ruolo) possono essere assegnati automaticamente o in futuro manualmente.
6. Alla fine del timer il cantiere si trasforma nell’edificio finale (es. Casa di legno pronta all’uso).

### Esempio di entry nel menu Costruzioni (MVP e subito dopo)

Per il primo periodo le entry principali saranno:

- **Deposito legna** (già presente, ma mostrabile nel menu in futuro)
  - Costo: legna.
  - Effetto: aumenta o definisce la capienza massima della legna.
  - Timer: breve.

- **Casa di legno (Tier 1)**
  - Costo: solo legna.
  - Effetto:
    - fornisce un posto letto per il villager;
    - permette di usare la meccanica di “riposo/dormire”.
  - Timer: medio (es. qualche minuto di tempo reale).

In fasi successive:
- **Casa legno + pietra (Tier 2)**
  - Costo: legna + pietra.
  - Effetto: riposo più efficiente, possibili bonus alla produttività.

- **Punti di riposo leggeri**
  - Costo: legna (e in futuro altre risorse leggere).
  - Effetto: il villager recupera un po’ di stanchezza senza tornare a casa.

### Regole di sblocco

Per mantenere il gioco leggibile e adatto a sessioni brevi:

- All’inizio il menu delle creazioni mostra **pochissime opzioni**:
  - Casa di legno;
  - (eventualmente) un solo oggetto di riposo base in una fase successiva.
- Nuove creazioni si sbloccano quando:
  - il giocatore ha completato certi “obiettivi soft” (es. raccogli X legna, costruisci la prima casa);
  - oppure quando viene introdotta una nuova risorsa o mestiere (es. minatore → pietra → nuovi edifici).

### Ritmo e UX

- Il menu deve essere apribile/chiudibile rapidamente, con poche informazioni per costruzione:
  - icona;
  - nome;
  - costo principale;
  - breve descrizione testuale.
- L’idea è permettere al giocatore di entrare, scegliere 1–2 creazioni, avviare i timer e uscire, lasciando che i villager lavorino in automatico.

### Fede e velocizzazione dei cantieri

La **fede** non è solo una statistica astratta, ma anche una risorsa speciale che il dio può spendere per manipolare il tempo delle costruzioni.

Per ogni cantiere attivo (es. Casa di legno):

- Il cantiere ha:
  - un timer totale (es. 15 minuti);
  - un tempo rimanente;
  - un costo in fede per intervenire.

- Il giocatore ha tre opzioni:
  1. **Aspettare** che il timer finisca naturalmente (gioco totalmente free).
  2. **Spendere fede** per:
     - completare istantaneamente la costruzione se il costo in fede è raggiunto;
     - oppure, nel caso di cantieri molto lunghi, **ridurre di molto il tempo rimanente** (es. -50% del tempo restante).
  3. In futuro: usare valuta premium per trasformare parte della fede o ottenere fede extra.

Linee guida:
- I cantieri corti (es. piccole costruzioni) possono essere completati subito con un costo in fede relativamente basso.
- I cantieri lunghi (edifici importanti) possono avere:
  - un’opzione “completa ora” molto costosa in fede;
  - oppure più opzioni di “riduzione tempo” (es. -25%, -50%) a costi crescenti.
- La fede deve rigenerarsi o essere guadagnata giocando (azioni buone, obiettivi, missioni giornaliere), in modo che il giocatore senta di avere sempre una scelta tra:
  - aspettare e tornare più tardi;
  - usare la fede accumulata per velocizzare il progresso quando ha poco tempo.

In termini di esperienza:
- i giocatori che vogliono solo “guardare il villaggio crescere” possono aspettare;
- i giocatori che vogliono vedere subito il risultato di una nuova costruzione possono spendere fede per piegare il tempo a loro favore.

### Come si genera la fede

La fede rappresenta quanto il villaggio crede (o teme) il dio, e deve essere guadagnata giocando, non solo comprata.

Per le prime versioni, la fede può essere generata da:

1. **Azioni benevole**
   - Aiutare i villager quando sono bloccati o esitano.
   - Dare strumenti chiave (es. la prima ascia) nei momenti giusti.
   - Salvare un villager da situazioni negative (in futuro: fame, pericoli, ecc.).
   - Costruire strutture che migliorano la loro vita (case, punti di riposo).

2. **Obiettivi completati (milestone)**
   - Raccogliere una certa quantità di legna per la prima volta.
   - Costruire la prima casa.
   - Riempire il deposito legna più volte.
   - Sbloccare un nuovo mestiere (es. minatore) o edificio importante.
   Ogni milestone può dare un “pacchetto” di fede come ricompensa.

3. **Interazione quotidiana (daily)**
   - Accedere al gioco almeno una volta al giorno.
   - Completare un piccolo “task del giorno” (es. “fai dormire il villager”, “avvia una costruzione”).
   Questo supporta il ritmo da pochi minuti al giorno e ricompensa la costanza.

### Fede e scelte morali

In futuro, la fede potrà anche essere influenzata dalla **moralità delle azioni**:

- Azioni “buone” (proteggere, curare, costruire per il bene dei villager)
  - aumentano la fede “positiva”;
- Azioni “cattive” (distruggere, eliminare senza motivo, terrorizzare)
  - potrebbero ridurre un tipo di fede e aumentare un’altra forma di “fede per paura”.

Nelle prime versioni:
- useremo un solo valore di fede, senza distinguere bene/male;
- introdurremo la distinzione solo quando il loop di base sarà stabile.


### Spingere oltre il limite: infortuni e morte (futuro)

In una fase successiva vogliamo dare al giocatore la possibilità di **forzare** i villager a lavorare anche quando sono stanchi, introducendo rischio e moralità:

- Se `energia` è sotto la soglia di stanchezza, ma il dio li costringe comunque a lavorare:
  - aumenta la probabilità di **infortunio** (il villager deve fermarsi per un po’ e non può lavorare);
  - in casi estremi, c’è una bassa probabilità di **morte per sfinimento**.

Effetti di design:
- Il giocatore “cattivo” può ottenere più lavoro a breve termine, ma:
  - rischia di perdere villager (meno forza lavoro);
  - può ridurre la fede o cambiare il tipo di fede (più paura, meno devozione positiva).
- Il giocatore “buono” accetta il ritmo naturale lavoro → riposo, limitando il rischio.

Nelle prime versioni questo sistema resta disattivato o semplificato; verrà attivato solo quando il loop base sarà stabile, per evitare frustrazione precoce.

### Limiti alle costruzioni

Per il tono rilassato e “sandbox” del gioco, non ci sono limiti rigidi al numero di case o costruzioni, finché il giocatore ha risorse sufficienti.

Regola base:
- Se il giocatore possiede i materiali necessari, può costruire quante case e quanti oggetti desidera, senza tetti artificiali per edificio.

Considerazioni:
- Alcune costruzioni potrebbero avere solo limiti “soft” di bilanciamento (es. costi crescenti, tempi di costruzione più lunghi), ma non blocchi duri del tipo “massimo 3 case”.
- Il numero di case influenza in modo naturale il villaggio:
  - più case = più posti letto e quindi più villager gestibili;
  - più punti di riposo (panche ecc.) = villager più efficienti senza dover sempre tornare a casa.

Obiettivo:
- Lasciare al giocatore la libertà di plasmare l’isola come vuole, purché raccolga le risorse necessarie, mantenendo l’esperienza adatta a sessioni brevi ma soddisfacenti.

## Note tecniche future (da implementare)

### Entrata fisica nella casa
Attualmente il villager raggiunge il GameObject `Door` (figlio della casa) e dorme lì fuori.
Per simulare l’entrata reale sarà necessario:
1. Posizionare `Door` davanti alla soglia (esterno)
2. Aggiungere un punto interno `SleepSpot` dentro la casa
3. Quando il villager arriva a `Door`: disabilitare il suo collider, teletrasportarlo a `SleepSpot`
4. Al risveglio: teletrasportarlo fuori e riabilitare il collider
Il `NavMeshObstacle` della casa impedisce l’accesso NavMesh diretto all’interno.

---

## Poteri Divini — Sistema Completo

Il menu dei poteri divini è un pannello laterale destro che si apre/chiude con freccia.
Il giocatore seleziona un potere e poi tocca il bersaglio nel mondo.

| Potere | Colore UI | Effetto Fede | Bersaglio | Descrizione |
|--------|-----------|-------------|-----------|-------------|
| Spawn Uomo | Blu | −20 | Terreno | Crea un villager maschio |
| Spawn Donna | Blu | −20 | Terreno | Crea un villager femmina |
| Spawn Cane | Grigio (bloccato) | −30 | — | Non disponibile in MVP |
| Spawn Gatto | Grigio (bloccato) | −30 | — | Non disponibile in MVP |
| ⚡ Smite | Rosso | −5 | Oggetto / Villager | Colpisce con fulmine, danneggia oggetti o stordisce/uccide villager |
| ✨ Ripara | Verde | +3 | Oggetto danneggiato | Ripristina lo stato intatto dell’oggetto |
| 💀 Revive | Viola | −15 | Villager morto | Riporta in vita un villager morto per stanchezza |

---

## Sistema Danneggiamento Oggetti (`DamageableObject.cs`)

Tutti gli edifici, arredi e costruzioni hanno il componente `DamageableObject` attaccato.

### Stati
- **Intact** — aspetto normale
- **Damaged** — materiale carbonizzato/bruciato + inclinazione casuale ±12° (animata)
- **Destroyed** — oggetto invisibile, spawn prefab macerie opzionale

### Progressione danno
- Primo **Smite** → stato Damaged
- Secondo **Smite** → stato Destroyed
- **Repair** in qualsiasi stato danneggiato → torna Intact

### Materiali danno (`Assets/_Project/Art/Materials/Damage/`)
- `mat_damaged_charred` — pietra annerita (edifici in pietra/chiesa)
- `mat_damaged_wood` — legno bruciato (mobili, panche)
- `mat_destroyed_rubble` — cenere scura con leggero glow ember

### Prefab con DamageableObject
**Chiesa** (`Assets/_Project/Prefabs/Church/`):
`church_exterior`, `church_interior`, `pew_medieval`, `presbytery_medieval`, `chandelier_medieval`, `candelabra_medieval`

**Arredo casa** (`Assets/_Project/Prefabs/Furniture/`):
`bed_medieval`, `chest_medieval`, `desk_medieval`, `stool_medieval`, `candle_medieval`, `wall_torch_medieval`

---

## Potere Smite — Effetto Fulmine VFX

Quando il dio usa Smite su un oggetto o villager, compare un fulmine visivo.

### Comportamento
- Un fulmine parte dall’alto (es. Y+10 sopra il bersaglio) e scende zigzagando fino al punto d’impatto
- Al contatto: flash bianco brillante + burst particelle gialle/arancioni
- Durata totale: ~0.3 secondi
- Il fulmine è un `LineRenderer` con 6–8 punti casuali tra start e target

### Implementazione (TODO)
- Script `LightningStrike.cs`: crea LineRenderer dinamico, anima in 2–3 frame, si autodistrugge
- Chiamato da `DivineActionSystem.OnObjectTapped()` e `OnVillagerTapped()` quando potere = Smite
- Opzionale: `AudioSource` con clip tuono breve

---

## Potere Revive — Riportare in vita i Villager

### Contesto
I villager possono morire per **stanchezza estrema** (energia = 0 dopo lungo lavoro senza riposo).
Questo non è un bug ma una meccanica di rischio per il "dio sadico".

### Comportamento Villager morto
- Energia arriva a 0 → villager entra in stato **Dead**
- Il corpo crolla (animazione fall o tween)
- Rimane visibile nel mondo come oggetto selezionabile
- La fede del villaggio scende di 20 (perdita di un abitante)

### Potere Revive
- Seleziona Revive dal menu poteri, tocca il corpo del villager morto
- Costo: 15 fede
- Effetto: villager torna in vita con energia al 50%, stato Idle
- VFX: particelle dorate attorno al corpo durante la resurrezione
- Se non viene revivato entro un certo tempo (futuro), il corpo scompare e il villager è perso definitivamente

---

## Asset Medievali Creati

### Arredo Casa (`Assets/_Project/Prefabs/Furniture/`)
| Prefab | Descrizione |
|--------|-------------|
| `bed_medieval` | Letto con telaio in legno scuro, materasso di paglia, cuscino grezzo |
| `chest_medieval` | Baule con fasce metalliche, serratura frontale |
| `desk_medieval` | Scrivania con ripiano inferiore e sgabello a 3 gambe |
| `stool_medieval` | Sgabello separato a 3 gambe tornite |
| `candle_medieval` | Candela con piattino in ferro e fiamma emissiva |
| `wall_torch_medieval` | Torcia a muro con braccio in ferro forgiato e fiamma |

### Chiesa (`Assets/_Project/Prefabs/Church/`)
| Prefab | Descrizione |
|--------|-------------|
| `church_exterior` | Navata + abside semicircolare + tetto doppia falda + portale ogivale + finestre |
| `church_bell_tower` | Campanile con guglia a 8 facce, campana in ferro, croce in cima |
| `church_interior` | Pavimento in pietra, 4 colonne per lato, archi ogivali, travi lignee, vetrate |
| `pew_medieval` | Panca con seduta, schienale, predellino e pannelli laterali decorati |
| `presbytery_medieval` | Piattaforma rialzata 3 gradini + altare con tovaglia rossa + croce + candelieri dorati |
| `chandelier_medieval` | Lampadario a corona con 6 candele, catena e bracci in ferro |
| `candelabra_medieval` | Candelabro alto da pavimento con tripode in ferro e candela |

### Note posizionamento
- La chiesa è composta da più prefab da assemblare nella scena:
  - `church_exterior` come guscio esterno
  - `church_interior` posizionato all’interno
  - `church_bell_tower` affiancato alla navata (lato sinistro o destro)
  - Panche, presbiterio, lampadario e candelabri come oggetti scena separati
- Tutti i prefab hanno `DamageableObject` e rispondono a Smite e Repair