using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows;

namespace FocusDesk.Services;

/// <summary>
/// Servizio per mostrare notifiche Toast native di Windows 10/11.
/// Usa ToastNotificationManagerCompat che supporta app non-packaged.
/// </summary>
public class NotificationService
{
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        try
        {
            // Registra handler per click sulla notifica (porta la finestra in foreground)
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var win = Application.Current.MainWindow;
                    if (win == null) return;
                    if (win.WindowState == WindowState.Minimized)
                        win.WindowState = WindowState.Normal;
                    win.Activate();
                    win.Topmost = true;
                    win.Topmost = false;
                });
            };
            _initialized = true;
        }
        catch
        {
            // Toast non disponibili su questo sistema
        }
    }

    /// <summary>Mostra una notifica Toast con titolo e messaggio.</summary>
    public void ShowToast(string title, string message, string? emoji = null)
    {
        try
        {
            var displayTitle = emoji != null ? $"{emoji} {title}" : title;

            new ToastContentBuilder()
                .AddText(displayTitle)
                .AddText(message)
                .SetToastDuration(ToastDuration.Short)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(8);
                    toast.SuppressPopup = false;
                });
        }
        catch
        {
            // Notifiche non disponibili — silent fallback
        }
    }

    /// <summary>Notifica fine pomodoro.</summary>
    public void NotifyPomodoroComplete(int sessionsDoneToday)
        => ShowToast(
            "Pomodoro completato!",
            $"Ottimo lavoro! Hai completato {sessionsDoneToday} pomodor{(sessionsDoneToday == 1 ? "o" : "i")} oggi.",
            "🍅");

    /// <summary>Notifica fine pausa.</summary>
    public void NotifyBreakComplete()
        => ShowToast(
            "Pausa terminata!",
            "Pronto per il prossimo pomodoro? Forza, ci sei!",
            "⏰");

    /// <summary>Notifica attivazione Focus Mode.</summary>
    public void NotifyFocusModeOn()
        => ShowToast(
            "Focus Mode attivata",
            "Distrazioni bloccate. Concentrati!",
            "🚀");

    /// <summary>Notifica disattivazione Focus Mode.</summary>
    public void NotifyFocusModeOff()
        => ShowToast(
            "Focus Mode disattivata",
            "Ottimo lavoro! Le restrizioni sono state rimosse.",
            "✅");

    public void Unregister()
    {
        try { ToastNotificationManagerCompat.Uninstall(); } catch { }
    }
}
