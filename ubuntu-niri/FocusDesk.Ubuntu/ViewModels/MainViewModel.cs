using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Data;
using FocusDesk.Models;
using FocusDesk.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Media;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using FocusDesk.Views;

namespace FocusDesk.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ─── Servizi ───────────────────────────────────────────────────────────────
    private readonly TimerService _timerService;
    private readonly StatsService _statsService;
    private readonly SettingsService _settingsService;
    private readonly HostsBlocker _hostsBlocker;
    private readonly DesktopService _desktopService;
    private readonly NotificationService _notificationService;
    public SoundService SoundService { get; }
    private readonly FocusAssistService _focusAssistService;

    // ─── Stato corrente sessione ────────────────────────────────────────────────
    private Session? _currentSession;
    private FocusOverlay? _focusOverlayWindow;

    // ─── Observable Properties ─────────────────────────────────────────────────
    [ObservableProperty] private SessionType _currentMode = SessionType.Focus;
    [ObservableProperty] private string _timerDisplay = "25:00";
    [ObservableProperty] private double _progress = 1.0;
    [ObservableProperty] private bool _isRunning = false;
    [ObservableProperty] private string _startButtonText = "Inizia";
    [ObservableProperty] private int _sessionsDoneToday = 0;
    [ObservableProperty] private int _sessionsInCycle = 0;
    [ObservableProperty] private TaskItem? _selectedTask = null;
    [ObservableProperty] private bool _isFocusModeActive = false;
    [ObservableProperty] private string _focusModeButtonText = "🚀 Avvia Focus Mode";
    [ObservableProperty] private AppSettings _settings;

    public AIPlannerViewModel AIPlannerVm { get; }

    public ObservableCollection<TaskItem> Tasks { get; } = new();

    // ─── Costruttore ───────────────────────────────────────────────────────────
    public MainViewModel()
    {
        _timerService = new TimerService();
        _statsService = new StatsService();
        _settingsService = new SettingsService();
        _hostsBlocker = new HostsBlocker();
        _desktopService = new DesktopService();
        _notificationService = new NotificationService();
        SoundService = new SoundService();
        _focusAssistService = new FocusAssistService();

        _notificationService.Initialize();

        _settings = _settingsService.Load();

        AIPlannerVm = new AIPlannerViewModel(this);

        _timerService.Tick += OnTimerTick;
        _timerService.Completed += OnTimerCompleted;

        // Imposta la durata iniziale
        _timerService.SetDuration(GetCurrentDuration());
        UpdateDisplay(GetCurrentDuration());

        _ = LoadDataAsync();
    }

    // ─── Comandi Timer ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleTimer()
    {
        if (Settings.PlaySounds) SoundService.PlayUiSound(Settings, "button.wav");

        if (IsRunning)
        {
            _timerService.Pause();
            SoundService.StopTicking();
            IsRunning = false;
            StartButtonText = "Riprendi";
        }
        else
        {
            if (_timerService.State == TimerState.Paused)
            {
                _timerService.Resume();
                SoundService.StartTicking(Settings);
            }
            else
            {
                StartSession();
            }
            IsRunning = true;
            StartButtonText = "Pausa";
        }
    }

    [RelayCommand]
    private void ResetTimer()
    {
        if (Settings.PlaySounds) SoundService.PlayUiSound(Settings, "button.wav");
        _timerService.Stop();
        SoundService.StopTicking();
        _timerService.SetDuration(GetCurrentDuration());
        IsRunning = false;
        StartButtonText = "Inizia";
        Progress = 1.0;
        _currentSession = null;
    }

    [RelayCommand]
    private void SwitchMode(string modeStr)
    {
        if (Settings.PlaySounds) SoundService.PlayUiSound(Settings, "button.wav");
        if (!Enum.TryParse<SessionType>(modeStr, out var mode)) return;
        _timerService.Stop();
        SoundService.StopTicking();
        _currentSession = null;
        IsRunning = false;
        StartButtonText = "Inizia";
        CurrentMode = mode;
        var duration = GetCurrentDuration();
        _timerService.SetDuration(duration);
        UpdateDisplay(duration);
        Progress = 1.0;
    }

    // ─── Focus Mode ────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ToggleFocusMode()
    {
        if (!IsFocusModeActive)
        {
            await ActivateFocusModeAsync();
        }
        else
        {
            await DeactivateFocusModeAsync();
        }
    }

    private async Task ActivateFocusModeAsync()
    {
        // Blocca i siti se abilitato nelle impostazioni
        if (Settings.EnableWebsiteBlocking)
        {
            await using var db = new AppDbContext();
            var domains = await db.BlockedSites
                .Where(s => s.IsEnabled)
                .Select(s => s.Domain)
                .ToListAsync();

            if (domains.Any())
            {
                try
                {
                    await _hostsBlocker.BlockSitesAsync(domains);
                }
                catch (UnauthorizedAccessException)
                {
                    _ = MessageBoxManager.GetMessageBoxStandard("Privilegi insufficienti",
                        "FocusDesk non ha i permessi per bloccare i siti web.\n" +
                        "Esegui l'app con sudo per il blocco.\n" +
                        "La Focus Mode e il timer si avvieranno comunque.",
                        ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
                }
                catch (Exception ex)
                {
                    _ = MessageBoxManager.GetMessageBoxStandard("Errore", $"Errore durante il blocco: {ex.Message}", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
                }
            }
        }

        IsFocusModeActive = true;
        FocusModeButtonText = "❌ Esci da Focus Mode";

        // Mostra overlay nello stesso processo (Desktop Isolation rimosso per permettere cambio schede)
        if (_focusOverlayWindow == null)
        {
            _focusOverlayWindow = new FocusDesk.Views.FocusOverlay(_timerService, () => _ = ToggleFocusMode());
            _focusOverlayWindow.Show();
        }
        
        if (Settings.ShowNotifications)
            _notificationService.NotifyFocusModeOn();
    }

    private async Task DeactivateFocusModeAsync()
    {
        if (Settings.EnableWebsiteBlocking && _hostsBlocker.IsCurrentlyBlocking)
        {
            try
            {
                await _hostsBlocker.UnblockAllAsync();
            }
            catch (Exception ex)
            {
                _ = MessageBoxManager.GetMessageBoxStandard("Errore", $"Errore durante lo sblocco: {ex.Message}").ShowAsync();
            }
        }

        if (_focusOverlayWindow != null)
        {
            var win = _focusOverlayWindow;
            _focusOverlayWindow = null; // Evita loop con OnExit
            win.Close();
        }

        IsFocusModeActive = false;
        FocusModeButtonText = "🚀 Avvia Focus Mode";

        if (Settings.ShowNotifications)
            _notificationService.NotifyFocusModeOff();
    }

    // ─── Gestione Task ─────────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectTask(TaskItem? task)
    {
        SelectedTask = task;
    }

    public async Task RefreshTasksAsync()
    {
        await LoadDataAsync();
    }

    // ─── Salva impostazioni ────────────────────────────────────────────────────
    public void SaveSettings()
    {
        _settingsService.Save(Settings);
        // Aggiorna la durata del timer se non è in corso
        if (!IsRunning && _timerService.State == TimerState.Stopped)
        {
            var duration = GetCurrentDuration();
            _timerService.SetDuration(duration);
            UpdateDisplay(duration);
        }
    }

    // ─── Helper privati ────────────────────────────────────────────────────────
    private void StartSession()
    {
        var duration = GetCurrentDuration();
        _timerService.Start(duration);

        if (Settings.PlaySounds)
        {
            SoundService.PlayUiSound(Settings, "button.wav");
        }

        if (CurrentMode == SessionType.Focus)
        {
            SoundService.StartTicking(Settings);
        }

        // Focus Assist (Do Not Disturb) during focus sessions
        if (CurrentMode == SessionType.Focus && Settings.EnableFocusAssist)
        {
            _focusAssistService.EnableForSession();
        }

        _currentSession = new Session
        {
            StartTime = DateTime.Now,
            Type = CurrentMode,
            DurationMinutes = (int)duration.TotalMinutes,
            TaskItemId = SelectedTask?.Id
        };
    }

    private void OnTimerTick(object? sender, TimeSpan remaining)
    {
        UpdateDisplay(remaining);
        var total = _timerService.TotalDuration.TotalSeconds;
        Progress = total > 0 ? remaining.TotalSeconds / total : 0;
    }

    private async void OnTimerCompleted(object? sender, EventArgs e)
    {
        IsRunning = false;
        StartButtonText = "Inizia";
        Progress = 0;

        if (_currentSession == null) return;

        _currentSession.EndTime = DateTime.Now;
        _currentSession.IsCompleted = true;

        try
        {
            await _statsService.SaveSessionAsync(_currentSession);

            if (CurrentMode == SessionType.Focus)
            {
                if (Settings.EnableFocusAssist)
                    _focusAssistService.RestoreAfterSession();

                SessionsDoneToday++;
                SessionsInCycle++;

                // Aggiorna il task corrente
                if (SelectedTask != null)
                {
                    await using var db = new AppDbContext();
                    var task = await db.TaskItems.FindAsync(SelectedTask.Id);
                    if (task != null)
                    {
                        task.CompletedPomodoros++;
                        await db.SaveChangesAsync();
                        SelectedTask.CompletedPomodoros = task.CompletedPomodoros;
                        OnPropertyChanged(nameof(SelectedTask));
                    }
                }

                // Suono notifica
                if (Settings.PlaySounds)
                    SoundService.PlayAlarm(Settings);

                // Notifica Windows
                if (Settings.ShowNotifications)
                    _notificationService.NotifyPomodoroComplete(SessionsDoneToday);

                // Auto-switch modalità
                if (SessionsInCycle >= Settings.SessionsBeforeLongBreak)
                {
                    SessionsInCycle = 0;
                    SwitchMode(nameof(SessionType.PausaLunga));
                }
                else
                {
                    SwitchMode(nameof(SessionType.PausaBreve));
                }

                if (Settings.AutoStartBreaks)
                    ToggleTimer();
            }
            else
            {
                // Pausa terminata
                if (Settings.PlaySounds)
                    SoundService.PlayAlarm(Settings);

                if (Settings.ShowNotifications)
                    _notificationService.NotifyBreakComplete();

                SwitchMode(nameof(SessionType.Focus));

                if (Settings.AutoStartFocus)
                    ToggleTimer();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error completing session: {ex}");
        }
        finally
        {
            _currentSession = null;
        }
    }

    private TimeSpan GetCurrentDuration() => CurrentMode switch
    {
        SessionType.Focus => TimeSpan.FromMinutes(Settings.FocusDuration),
        SessionType.PausaBreve => TimeSpan.FromMinutes(Settings.ShortBreakDuration),
        SessionType.PausaLunga => TimeSpan.FromMinutes(Settings.LongBreakDuration),
        _ => TimeSpan.FromMinutes(25)
    };

    private void UpdateDisplay(TimeSpan time)
    {
        TimerDisplay = $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private async Task LoadDataAsync()
    {
        SessionsDoneToday = await _statsService.GetTodaySessionsAsync();

        await using var db = new AppDbContext();
        var tasks = await db.TaskItems
            .Where(t => !t.IsCompleted)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        Dispatcher.UIThread.Post(() =>
        {
            Tasks.Clear();
            foreach (var t in tasks) Tasks.Add(t);
        });
    }
}
