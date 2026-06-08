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
