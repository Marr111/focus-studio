# FocusDesk

FocusDesk è un'applicazione desktop originariamente pensata per Windows e ora disponibile anche nativamente per Linux (Niri/Ubuntu), sviluppata per massimizzare la produttività tramite sessioni di Focus (Pomodoro) integrate con strumenti avanzati per ridurre le distrazioni.

## Architettura del Repository
Il progetto è suddiviso in due versioni specifiche per sistema operativo:
- **`windows/`**: Contiene la versione originale basata su **WPF**, ottimizzata per Windows con gestione dei Virtual Desktops tramite API Win32, `MediaPlayer` integrato e blocco hosts nativo.
- **`ubuntu/`**: Contiene il porting scritto in **Avalonia UI**, con servizi di sistema specificamente adattati per l'ambiente Linux:
  - Gestione workspace tramite il compositor Wayland **Niri** (`niri msg`).
  - Blocco dei siti web su `/etc/hosts` con escalation dei privilegi tramite **pkexec**.
  - Notifiche inviate tramite **notify-send**.
  - Do Not Disturb integrato per i demoni di notifica **dunst** e **mako**.
  - Motore audio cross-platform basato su **NetCoreAudio**.

## Caratteristiche
- **Timer Pomodoro:** Sessioni di focus, pause brevi e pause lunghe configurabili.
- **Isolamento Desktop (Focus Mode):** Spostamento in un workspace isolato in cui girano solo le app permesse.
- **Blocco Siti Web:** Possibilità di bloccare automaticamente l'accesso a siti web distrattivi durante le sessioni di lavoro (gestione automatica del file hosts).
- **Effetti Audio Avanzati:** Include ticchettii continui (rumore bianco, browniano, suoni di orologi) e sveglie di notifica, tutto riproducibile in loop.
- **Statistiche Dettagliate:** Grafici per monitorare i tuoi periodi di maggiore produttività.

## Requisiti
- **Windows**: Windows 10 o superiore.
- **Ubuntu/Linux**: Compositor Wayland **Niri** (per isolamento desktop), `pkexec`, `notify-send`, `dunst` o `mako`.
- .NET 8.0 SDK

## Come compilare

### Versione Windows
Spostati nella cartella `windows` ed esegui:
```bash
cd windows
dotnet build
```
*(Nota: la Focus Mode su Windows richiede di avviare l'applicazione come amministratore).*

### Versione Ubuntu
Spostati nella cartella `ubuntu` ed esegui:
```bash
cd ubuntu/FocusDesk.Ubuntu
dotnet build
```

## Sincronizzazione Dati (Google Drive)

FocusDesk supporta la sincronizzazione automatica dello storico delle sessioni tra diversi computer (es. Windows e Ubuntu) appoggiandosi a Google Drive. Il sistema utilizza internamente **Rclone** per trasferire i dati in modo invisibile, senza richiedere l'uso della Google Cloud Console.

### Meccanismo
L'applicazione gestisce in totale autonomia il file `focusdesk.db`. La sincronizzazione avviene così:
- **Download all'avvio**: Prima ancora di caricare l'interfaccia, l'app interroga Drive e scarica il database più aggiornato in locale.
- **Upload al salvataggio**: Ogni volta che viene completata un'azione (Pomodoro terminato, task aggiornato, ecc.), i cambiamenti vengono salvati localmente e subito dopo ricaricati su Google Drive in background.
- **Prima esecuzione sicura**: Se la cartella Drive è vuota ma in locale possiedi già dei salvataggi, il sistema carica la versione locale su Drive anziché sovrascriverla o perderla.

### Configurazione di Rclone
Per far sì che l'applicazione possa comunicare con Drive, devi configurare `rclone` sul tuo PC creando un collegamento (remote) chiamato rigorosamente **`gdrive`**.

#### 1. Installazione
- **Ubuntu/Linux**: Esegui `sudo apt install rclone` dal terminale.
- **Windows**: Scaricalo dal [sito ufficiale](https://rclone.org/) oppure, da PowerShell, esegui `winget install Rclone.Rclone`.

#### 2. Configurazione Rapida
Apri il terminale (o PowerShell) e avvia la procedura guidata con:
```bash
rclone config
```
Poi segui questi passaggi:
1. Premi **`n`** per creare un "New remote".
2. Chiamalo esattamente: **`gdrive`** e premi Invio.
3. Seleziona **Google Drive** dalla lista proposta (digita il numero corrispondente, ad es. `18` o la parola `drive`).
4. Lascia vuoti sia `client_id` che `client_secret` (premi Invio).
5. Alla richiesta dello **"Scope"**, digita **`1`** (Full access) per permettere la scrittura sulla tua cartella.
6. Salta le configurazioni avanzate premendo Invio.
7. Alla domanda `Use auto config?`, rispondi **`y`**. Si aprirà il browser: accedi col tuo account Google e clicca **Consenti**.
8. Alla richiesta `Configure this as a Shared Drive (Team Drive)?`, rispondi **`n`**.
9. Conferma il riepilogo con **`y`** e, infine, premi **`q`** per uscire ("Quit config").

Tutto pronto! Ora l'app si sincronizzerà in modo invisibile e automatico ad ogni utilizzo.
