using System.Diagnostics;

namespace FocusDesk.Services;

public class DesktopService : IDisposable
{
    public bool IsActive { get; private set; }

    public bool EnterFocusDesktop(string overlayExePath, IEnumerable<string> whitelistPaths)
    {
        try
        {
            // Focus on a new workspace in Niri
            Process.Start(new ProcessStartInfo
            {
                FileName = "niri",
                Arguments = "msg action focus-workspace-down",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            IsActive = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DesktopService] Errore EnterFocusDesktop: {ex.Message}");
            return false;
        }
    }

    public void ExitFocusDesktop()
    {
        if (!IsActive) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "niri",
                Arguments = "msg action focus-workspace-up",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }

        IsActive = false;
    }

    public void LaunchCommandOnDesktop(string command)
    {
        if (!IsActive) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
    }

    public void Dispose() => ExitFocusDesktop();
}
