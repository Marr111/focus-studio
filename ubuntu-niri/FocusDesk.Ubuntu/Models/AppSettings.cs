using System;

namespace FocusDesk.Models;

public class AppSettings
{
    public int FocusDuration { get; set; } = 25;
    public int ShortBreakDuration { get; set; } = 5;
    public int LongBreakDuration { get; set; } = 15;
    public int SessionsBeforeLongBreak { get; set; } = 4;
    public bool PlaySounds { get; set; } = true;
    public bool EnableTickingSound { get; set; } = false;
    public string SelectedTickingSound { get; set; } = "ticking-fast.mp3";
    public bool EnableAlarmSound { get; set; } = true;
    public string SelectedAlarmSound { get; set; } = "alarm-bell.mp3";
    public bool EnableUiSounds { get; set; } = true;
    public bool AutoStartBreaks { get; set; } = false;
    public bool AutoStartFocus { get; set; } = false;
    public bool EnableWebsiteBlocking { get; set; } = false;
    public bool EnableDesktopIsolation { get; set; } = false;
    public bool EnableFocusAssist { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public string GeminiApiKey { get; set; } = "";
    public double Volume { get; set; } = 100; // Nuova proprietà per il volume
}
