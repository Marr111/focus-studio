# FocusDesk

> **La tua postazione di lavoro senza distrazioni.** Un'app desktop nativa per Windows e Linux che unisce il metodo Pomodoro, l'isolamento del desktop, il blocco dei siti e la sincronizzazione cloud — tutto in un'unica soluzione.

---

## Cos'è FocusDesk?

FocusDesk nasce da un'idea semplice: quando lavori, devi davvero lavorare. Nessuna notifica, nessun social, nessun browser aperto in background.

L'applicazione combina il **metodo Pomodoro** (sessioni di focus alternate a pause) con strumenti avanzati come l'isolamento del workspace, il blocco automatico di siti web distrattivi, suoni ambientali studiati per la concentrazione e statistiche dettagliate sulla produttività. Il tutto disponibile nativamente su **Windows** (WPF) e **Ubuntu/Linux con Niri** (Avalonia UI), con sincronizzazione trasparente dei dati via **Google Drive**.

---

## ✨ Punti di Forza

### 🖥️ Doppio Porting Nativo — Non un Compromesso
La maggior parte delle app di produttività su Linux sono port scadenti o app web travestite. FocusDesk è scritto con framework **nativi per ogni sistema operativo**:
- Su **Windows** usa **WPF** per un'integrazione profonda con le API Win32 e i Virtual Desktop.
- Su **Ubuntu/Niri** usa **Avalonia UI**, un framework moderno e cross-platform che garantisce un look nativo e prestazioni elevate.

Questo significa che ogni versione sfrutta al massimo le API di sistema disponibili, senza compromessi.

---

### 🔒 Focus Mode — Isolamento Totale del Workspace
Quando avvii una sessione, FocusDesk non si limita a impostare un timer: **sposta fisicamente il tuo desktop** in un workspace dedicato e isolato, dove girano solo le applicazioni che tu hai esplicitamente autorizzato.

- **Windows**: Utilizza le API Win32 e `IVirtualDesktopManager` per creare e gestire Virtual Desktop in modo programmatico.
- **Linux/Niri**: Invia comandi al compositor Wayland tramite `niri msg` per creare un workspace dedicato e spostare le finestre.

Il risultato è lo stesso: tutto ciò che non è lavoro sparisce dalla vista. Nessuna forza di volontà richiesta.

---

### 🌐 Blocco Siti Web Automatico
Durante le sessioni di focus, FocusDesk modifica automaticamente il file **`/etc/hosts`** (o l'equivalente Windows) per bloccare i siti che tu stesso hai inserito nella lista nera. Al termine della sessione, il blocco viene rimosso altrettanto automaticamente.

- Su **Windows** il file hosts è gestito tramite lo script `fix_hosts.bat` con elevazione dei privilegi.
- Su **Linux** usa `pkexec` per richiedere i permessi di root senza mai aprire un terminale.

Nessun plugin per browser. Nessun proxy. Il blocco funziona a livello di sistema operativo, per ogni browser e ogni app.

---

### 🎵 Motore Audio Avanzato
FocusDesk include un sistema audio sofisticato con:
- **Suoni ambientali in loop**: rumore bianco, rumore browniano, ticchettii di orologio e altri suoni progettati per favorire la concentrazione (stile "focus music").
- **Sveglie di notifica** al termine di ogni sessione o pausa.
- **Controllo volume indipendente** per suoni ambientali e notifiche.

Su Windows usa `MediaPlayer` nativo; su Linux usa **NetCoreAudio** per un supporto cross-platform trasparente.

---

### 📊 Statistiche Dettagliate
Non basta lavorare — bisogna capire *come* si lavora. FocusDesk registra ogni sessione completata e offre **grafici interattivi** per visualizzare:
- Le ore di maggiore produttività nella giornata.
- Il trend settimanale e mensile delle sessioni.
- Il numero di Pomodori completati nel tempo.

I dati sono salvati localmente in un database SQLite (`focusdesk.db`) e sincronizzati automaticamente su Google Drive.

---

### ☁️ Sincronizzazione Trasparente via Google Drive
Lavori su più computer? I tuoi dati ti seguono. FocusDesk usa **Rclone** per sincronizzare silenziosamente il database tra tutti i tuoi dispositivi:

- **Download all'avvio**: Prima ancora di mostrare l'interfaccia, l'app scarica la versione più aggiornata del database da Drive.
- **Upload dopo ogni azione**: Ogni Pomodoro completato, ogni task aggiornato, viene immediatamente salvato in cloud.
- **Sicurezza al primo avvio**: Se il Drive è vuoto ma hai già dei dati in locale, il sistema carica quelli locali su Drive senza sovrascrivere nulla.

---

### 📋 Gestione Task Integrata
Tieni traccia di cosa devi fare direttamente nell'app, senza dover aprire un'altra applicazione. La vista Task ti permette di creare, completare e organizzare le attività associate alle tue sessioni di lavoro.

---

### 🔕 Do Not Disturb Nativo
Su Linux, FocusDesk interagisce direttamente con i demoni di notifica **dunst** e **mako** per silenziare tutte le notifiche di sistema durante le sessioni di focus. Le notifiche tornano automaticamente al termine della sessione o della pausa.

---

## 🏗️ Architettura del Repository

```
focus-studio/
├── windows/               # Versione WPF per Windows
│   ├── FocusDesk/
│   │   ├── Views/         # Finestre e pagine (XAML)
│   │   ├── ViewModels/    # Logica UI (MVVM)
│   │   ├── Services/      # Servizi di sistema (audio, hosts, desktop, sync...)
│   │   ├── Models/        # Modelli dati
│   │   └── Data/          # Accesso al database SQLite (EF Core)
│   └── FocusDesk.Tests/   # Unit test
│
└── ubuntu-niri/           # Versione Avalonia UI per Linux (Niri/Wayland)
    └── FocusDesk.Ubuntu/
        ├── Views/         # Finestre e pagine (AXAML)
        ├── ViewModels/    # Logica UI (MVVM)
        ├── Services/      # Servizi adattati per Linux
        ├── Models/        # Modelli dati
        └── Data/          # Accesso al database SQLite (EF Core)
```

Entrambe le versioni seguono il pattern **MVVM** e condividono la stessa struttura logica, pur usando API di sistema completamente diverse.

---

## ⚙️ Requisiti

### Comuni a entrambe le versioni
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Rclone](https://rclone.org/) *(opzionale, solo per la sincronizzazione Google Drive)*

### Windows
- Windows 10 (build 1903+) o Windows 11
- La Focus Mode richiede l'avvio dell'applicazione **come Amministratore** (per la gestione dei Virtual Desktop e del file hosts).

### Ubuntu / Linux
- Compositor Wayland **[Niri](https://github.com/YaLTeR/niri)** (richiesto per la Focus Mode con isolamento workspace)
- `pkexec` (per la modifica del file hosts senza aprire un terminale root)
- `notify-send` (per le notifiche di sistema)
- `dunst` oppure `mako` (per la funzione Do Not Disturb)

---

## 🚀 Installazione e Compilazione

### Windows

**1. Clona il repository:**
```powershell
git clone https://github.com/Marr111/focus-studio.git
cd focus-studio
```

**2. Spostati nella cartella Windows e compila:**
```powershell
cd windows
dotnet build
```

**3. Avvia l'applicazione:**
```powershell
dotnet run --project FocusDesk
```

> **Nota:** Per abilitare la **Focus Mode** (isolamento desktop e blocco hosts), avvia l'applicazione come **Amministratore**: tasto destro sull'eseguibile → *Esegui come amministratore*.

Per creare un eseguibile standalone da distribuire:
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

---

### Ubuntu / Linux (con Niri)

**1. Installa le dipendenze di sistema:**
```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0 libx11-dev libice-dev libsm-dev libxt-dev
```

> Se il tuo sistema non ha .NET nei repository ufficiali, segui la [guida ufficiale Microsoft](https://learn.microsoft.com/it-it/dotnet/core/install/linux-ubuntu).

**2. Assicurati di avere `notify-send` e un demone di notifica:**
```bash
sudo apt install -y libnotify-bin dunst   # oppure mako-notifier al posto di dunst
```

**3. Clona il repository:**
```bash
git clone https://github.com/Marr111/focus-studio.git
cd focus-studio
```

**4. Spostati nella cartella Ubuntu e compila:**
```bash
cd ubuntu-niri/FocusDesk.Ubuntu
dotnet build
```

**5. Avvia l'applicazione:**
```bash
dotnet run
```

Per creare un eseguibile standalone:
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

---

## ☁️ Configurazione della Sincronizzazione (Google Drive)

La sincronizzazione è **opzionale** ma altamente consigliata se usi FocusDesk su più macchine.

### 1. Installa Rclone

**Windows (PowerShell):**
```powershell
winget install Rclone.Rclone
```
Oppure scarica l'installer dal [sito ufficiale di Rclone](https://rclone.org/downloads/).

**Ubuntu/Linux:**
```bash
sudo apt install rclone
```

### 2. Configura il Remote Google Drive

Apri il terminale e lancia la procedura guidata:
```bash
rclone config
```

Segui questi passaggi nella procedura interattiva:

| Step | Azione |
|------|--------|
| `n` | Crea un **New remote** |
| Nome | Inserisci esattamente **`gdrive`** (il nome deve essere questo) |
| Tipo | Seleziona **Google Drive** (digita il numero corrispondente) |
| `client_id` | Lascia vuoto → Invio |
| `client_secret` | Lascia vuoto → Invio |
| Scope | Digita **`1`** (Full access) |
| Configurazione avanzata | Salta → Invio |
| `Use auto config?` | Digita **`y`** → si aprirà il browser per il login Google |
| Team Drive? | Digita **`n`** |
| Conferma | Digita **`y`** → poi **`q`** per uscire |

Dopo il login con Google, la sincronizzazione sarà attiva e completamente automatica ad ogni avvio.

---

## 🤝 Contribuire

Pull request e issue sono benvenute! Se trovi un bug o hai un'idea per una nuova funzionalità, apri una issue su GitHub.

---

## 📄 Licenza

Distribuito sotto licenza MIT. Vedi il file `LICENSE` per i dettagli.
