using System.Diagnostics;

namespace FocusDesk.Services;

public class FocusAssistService
{
    private bool _wasEnabledByUs;

    public void EnableForSession()
    {
        _wasEnabledByUs = true;
        RunCommand("dunstctl set-paused true");
        RunCommand("makoctl mode -a do-not-disturb");
    }

    public void RestoreAfterSession()
    {
        if (_wasEnabledByUs)
        {
            RunCommand("dunstctl set-paused false");
            RunCommand("makoctl mode -r do-not-disturb");
            _wasEnabledByUs = false;
        }
    }

    private void RunCommand(string cmd)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{cmd}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
    }
}
