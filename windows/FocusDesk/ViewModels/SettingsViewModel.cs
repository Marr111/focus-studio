using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Data;
using FocusDesk.Models;
using FocusDesk.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace FocusDesk.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;

    // ─── Timer ────────────────────────────────────────────────────────────────
    [ObservableProperty] private int _focusDuration;
    [ObservableProperty] private int _shortBreakDuration;
    [ObservableProperty] private int _longBreakDuration;
    [ObservableProperty] private int _sessionsBeforeLongBreak;

    // ─── Comportamento ────────────────────────────────────────────────────────
    [ObservableProperty] private bool _autoStartBreaks;
    [ObservableProperty] private bool _autoStartFocus;
    [ObservableProperty] private bool _showNotifications;
    [ObservableProperty] private bool _minimizeToTray;

    // ─── Suoni ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _enableTickingSound;
    [ObservableProperty] private string _selectedTickingSound = "";
    [ObservableProperty] private bool _enableAlarmSound;
    [ObservableProperty] private string _selectedAlarmSound = "";
    [ObservableProperty] private bool _enableUiSounds;
    [ObservableProperty] private double _volume;

    public ObservableCollection<string> AvailableTickingSounds { get; } = new();
    public ObservableCollection<string> AvailableAlarmSounds { get; } = new();

    // ─── Focus Mode ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _enableDesktopIsolation;
    [ObservableProperty] private bool _enableWebsiteBlocking;
    [ObservableProperty] private bool _enableFocusAssist;

    // ─── Whitelist app ────────────────────────────────────────────────────────
    public ObservableCollection<AppWhitelistEntry> WhitelistApps { get; } = new();

    // ─── Siti bloccati ────────────────────────────────────────────────────────
    public ObservableCollection<BlockedSite> BlockedSites { get; } = new();
    [ObservableProperty] private string _newBlockedDomain = string.Empty;

    public SettingsViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        LoadFromSettings(mainVm.Settings);
        _ = LoadDbDataAsync();
    }

    private void LoadFromSettings(AppSettings s)
    {
        FocusDuration = s.FocusDuration;
        ShortBreakDuration = s.ShortBreakDuration;
        LongBreakDuration = s.LongBreakDuration;
        SessionsBeforeLongBreak = s.SessionsBeforeLongBreak;
        
        EnableTickingSound = s.EnableTickingSound;
        SelectedTickingSound = s.SelectedTickingSound;
        EnableAlarmSound = s.EnableAlarmSound;
        SelectedAlarmSound = s.SelectedAlarmSound;
        EnableUiSounds = s.EnableUiSounds;
        Volume = s.Volume;
        
        AutoStartBreaks = s.AutoStartBreaks;
        AutoStartFocus = s.AutoStartFocus;
        ShowNotifications = s.ShowNotifications;
        MinimizeToTray = s.MinimizeToTray;
        EnableDesktopIsolation = s.EnableDesktopIsolation;
        EnableWebsiteBlocking = s.EnableWebsiteBlocking;
        EnableFocusAssist = s.EnableFocusAssist;

        LoadAvailableSounds();
    }

    private void LoadAvailableSounds()
    {
        AvailableTickingSounds.Clear();
        AvailableAlarmSounds.Clear();

        try
        {
            var soundsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds");
            if (System.IO.Directory.Exists(soundsDir))
            {
                var files = System.IO.Directory.GetFiles(soundsDir, "*.*")
                    .Where(f => f.EndsWith(".mp3") || f.EndsWith(".wav"))
                    .Select(System.IO.Path.GetFileName)
                    .ToList();

                foreach (var file in files)
                {
                    if (file!.StartsWith("ticking"))
                        AvailableTickingSounds.Add(file);
                    else if (file.StartsWith("alarm") || file.StartsWith("focus_") || file.StartsWith("break_"))
                        AvailableAlarmSounds.Add(file);
                }
            }
            
            // Assicurati che i valori selezionati siano presenti
            if (!AvailableTickingSounds.Contains(SelectedTickingSound) && AvailableTickingSounds.Any())
                SelectedTickingSound = AvailableTickingSounds.First();
            
            if (!AvailableAlarmSounds.Contains(SelectedAlarmSound) && AvailableAlarmSounds.Any())
                SelectedAlarmSound = AvailableAlarmSounds.First();
        }
        catch { }
    }

    [RelayCommand]
    private void Save()
    {
        var s = _mainVm.Settings;
        s.FocusDuration = Math.Clamp(FocusDuration, 1, 120);
        s.ShortBreakDuration = Math.Clamp(ShortBreakDuration, 1, 60);
        s.LongBreakDuration = Math.Clamp(LongBreakDuration, 1, 60);
        s.SessionsBeforeLongBreak = Math.Clamp(SessionsBeforeLongBreak, 1, 10);
        
        s.EnableTickingSound = EnableTickingSound;
        s.SelectedTickingSound = SelectedTickingSound;
        s.EnableAlarmSound = EnableAlarmSound;
        s.SelectedAlarmSound = SelectedAlarmSound;
        s.EnableUiSounds = EnableUiSounds;
        s.Volume = Volume;
        
        s.AutoStartBreaks = AutoStartBreaks;
        s.AutoStartFocus = AutoStartFocus;
        s.ShowNotifications = ShowNotifications;
        s.MinimizeToTray = MinimizeToTray;
        s.EnableDesktopIsolation = EnableDesktopIsolation;
        s.EnableWebsiteBlocking = EnableWebsiteBlocking;
        s.EnableFocusAssist = EnableFocusAssist;

        _mainVm.SaveSettings();
        MessageBox.Show("Impostazioni salvate!", "FocusDesk",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task AddBlockedSite()
    {
        var domain = HostsBlocker.NormalizeDomain(NewBlockedDomain);

        if (string.IsNullOrEmpty(domain)) return;

        var site = new BlockedSite { Domain = domain, IsEnabled = true, Category = "Custom" };
        await using var db = new AppDbContext();
        db.BlockedSites.Add(site);
        await db.SaveChangesAsync();

        BlockedSites.Add(site);
        NewBlockedDomain = string.Empty;
    }

    [RelayCommand]
    private async Task RemoveBlockedSite(BlockedSite site)
    {
        BlockedSites.Remove(site);
        await using var db = new AppDbContext();
        db.BlockedSites.Remove(site);
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task ToggleSiteEnabled(BlockedSite site)
    {
        site.IsEnabled = !site.IsEnabled;
        await using var db = new AppDbContext();
        db.BlockedSites.Update(site);
        await db.SaveChangesAsync();
        OnPropertyChanged(nameof(BlockedSites));
    }

    [RelayCommand]
    private async Task AddWhitelistApp()
    {
        var window = new FocusDesk.Views.AppSelectorWindow();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true && window.SelectedExecutable != null)
        {
            var entry = new AppWhitelistEntry
            {
                ExecutablePath = window.SelectedExecutable,
                DisplayName = window.SelectedName!,
                SortOrder = WhitelistApps.Count
            };

            await using var db = new AppDbContext();
            db.WhitelistEntries.Add(entry);
            await db.SaveChangesAsync();

            WhitelistApps.Add(entry);
        }
    }

    [RelayCommand]
    private async Task RemoveWhitelistApp(AppWhitelistEntry entry)
    {
        WhitelistApps.Remove(entry);
        await using var db = new AppDbContext();
        db.WhitelistEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task AddSocialPreset()
        => await AddPresetSites(["facebook.com", "instagram.com", "twitter.com", "x.com", "tiktok.com", "linkedin.com"], "Social");

    [RelayCommand]
    private async Task AddNewsPreset()
        => await AddPresetSites(["reddit.com", "hackernews.com", "news.ycombinator.com", "corriere.it", "repubblica.it"], "News");

    [RelayCommand]
    private async Task AddVideoPreset()
        => await AddPresetSites(["youtube.com", "twitch.tv", "netflix.com", "primevideo.com"], "Video");

    private async Task AddPresetSites(string[] domains, string category)
    {
        await using var db = new AppDbContext();
        var added = false;
        foreach (var domain in domains)
        {
            if (BlockedSites.Any(s => s.Domain == domain)) continue;
            if (await db.BlockedSites.AnyAsync(s => s.Domain == domain)) continue;
            var site = new BlockedSite { Domain = domain, Category = category, IsEnabled = true };
            db.BlockedSites.Add(site);
            
            Application.Current.Dispatcher.Invoke(() => {
                if (!BlockedSites.Any(s => s.Domain == domain))
                    BlockedSites.Add(site);
            });
            added = true;
        }
        if (added)
            await db.SaveChangesAsync();
    }

    private async Task LoadDbDataAsync()
    {
        await using var db = new AppDbContext();

        var sites = await db.BlockedSites.OrderBy(s => s.Category).ThenBy(s => s.Domain).ToListAsync();
        var apps = await db.WhitelistEntries.OrderBy(a => a.SortOrder).ToListAsync();

        Application.Current.Dispatcher.Invoke(() =>
        {
            BlockedSites.Clear();
            foreach (var s in sites) BlockedSites.Add(s);

            WhitelistApps.Clear();
            foreach (var a in apps) WhitelistApps.Add(a);
        });
    }

    partial void OnSelectedTickingSoundChanged(string value)
    {
        if (EnableTickingSound)
            _ = _mainVm.SoundService.PlayPreviewAsync(value, 3000); // 3 seconds preview
    }

    partial void OnSelectedAlarmSoundChanged(string value)
    {
        if (EnableAlarmSound)
            _ = _mainVm.SoundService.PlayPreviewAsync(value, 2000); // 2 seconds preview
    }

    partial void OnVolumeChanged(double value)
    {
        _mainVm.SoundService.UpdateVolume((byte)value);
    }
}
