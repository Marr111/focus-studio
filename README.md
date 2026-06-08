# FocusDesk

FocusDesk è un'applicazione desktop per Windows, pensata per massimizzare la produttività tramite sessioni di Focus (Pomodoro) integrate con strumenti avanzati per ridurre le distrazioni.

## Caratteristiche
- **Timer Pomodoro:** Sessioni di focus, pause brevi e pause lunghe configurabili.
- **Isolamento Desktop (Focus Mode):** Creazione di un Virtual Desktop isolato in cui girano solo le app permesse (Whitelist).
- **Blocco Siti Web:** Possibilità di bloccare automaticamente l'accesso a siti web distrattivi durante le sessioni di lavoro.
- **Effetti Audio Avanzati:** Include ticchettii continui (rumore bianco, browniano, ecc.) e suoni di notifica (sveglie) per segnare l'inizio o la fine delle sessioni.
- **Statistiche Dettagliate:** Grafici orari e settimanali (tramite LiveCharts) per monitorare i tuoi periodi di maggiore produttività.

## Requisiti
- Windows 10 o superiore
- .NET 8.0 SDK

## Come compilare
Esegui da terminale:
```bash
dotnet build
```
Oppure apri `FocusDesk.sln` con Visual Studio 2022 e avvia la compilazione. L'applicazione richiede i permessi di amministratore per poter operare correttamente sul file hosts (per il blocco dei siti) e sulla gestione dei desktop.
