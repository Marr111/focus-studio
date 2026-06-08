using System.Diagnostics;

namespace FocusDesk.Services;

public class NotificationService
{
    public void Initialize() { }

    public void NotifyPomodoroComplete(int sessionCount)
    {
        RunNotifySend("FocusDesk", $"Pomodoro completato! Sessioni oggi: {sessionCount}");
    }

    public void NotifyBreakComplete()
    {
        RunNotifySend("FocusDesk", "Pausa terminata! Pronto per ricominciare?");
    }

    public void NotifyFocusModeOn()
    {
        RunNotifySend("Focus Mode", "Focus Mode attivata. Siti e distrazioni bloccati.");
    }

    public void NotifyFocusModeOff()
    {
        RunNotifySend("Focus Mode", "Focus Mode disattivata.");
    }

    private void RunNotifySend(string title, string message)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"\"{title}\" \"{message}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
    }
}
