using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusDesk.Services;

/// <summary>
/// Gestisce la creazione e navigazione tra desktop virtuali Windows via Win32 API.
/// </summary>
public class DesktopService : IDisposable
{
    #region Win32 Imports

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateDesktop(
        string lpszDesktop,
        IntPtr lpszDevice,
        IntPtr pDevmode,
        uint dwFlags,
        uint dwDesiredAccess,
        IntPtr lpsa);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SwitchDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetThreadDesktop(uint dwThreadId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(
        string? lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize;
        public uint dwXCountChars, dwYCountChars;
        public uint dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    private const uint DESKTOP_ALL_ACCESS = 0x01FF;

    #endregion

    private IntPtr _focusDesktop = IntPtr.Zero;
    private IntPtr _originalDesktop = IntPtr.Zero;
    private readonly List<uint> _focusProcessIds = new();

    public bool IsActive { get; private set; }

    /// <summary>
    /// Crea il desktop Focus e avvia il processo overlay su di esso.
    /// </summary>
    public bool EnterFocusDesktop(string overlayExePath, IEnumerable<string> whitelistPaths)
    {
        try
        {
            _originalDesktop = GetThreadDesktop(GetCurrentThreadId());
            _focusDesktop = CreateDesktop("FocusDesk", IntPtr.Zero, IntPtr.Zero, 0, DESKTOP_ALL_ACCESS, IntPtr.Zero);

            if (_focusDesktop == IntPtr.Zero)
                return false;

            // Avvia la barra delle applicazioni (explorer)
            LaunchOnDesktop("explorer.exe", "", "FocusDesk");

            // Avvia il processo overlay sul desktop focus
            var args = string.Join("|", whitelistPaths);
            LaunchOnDesktop(overlayExePath, args, "FocusDesk");

            SwitchDesktop(_focusDesktop);
            IsActive = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DesktopService] Errore EnterFocusDesktop: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Torna al desktop originale.
    /// </summary>
    public void ExitFocusDesktop()
    {
        if (!IsActive) return;

        if (_originalDesktop != IntPtr.Zero)
            SwitchDesktop(_originalDesktop);

        // Termina i processi avviati nel desktop focus
        foreach (var pid in _focusProcessIds)
        {
            try
            {
                Process.GetProcessById((int)pid).Kill();
            }
            catch { /* processo già terminato */ }
        }
        _focusProcessIds.Clear();

        if (_focusDesktop != IntPtr.Zero)
        {
            CloseDesktop(_focusDesktop);
            _focusDesktop = IntPtr.Zero;
        }

        IsActive = false;
    }

    public void LaunchCommandOnDesktop(string command)
    {
        if (!IsActive) return;
        var cmdPath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
        LaunchOnDesktop(cmdPath, $"/c {command}", "FocusDesk");
    }

    private void LaunchOnDesktop(string exePath, string args, string desktopName)
    {
        var si = new STARTUPINFO
        {
            cb = Marshal.SizeOf<STARTUPINFO>(),
            lpDesktop = desktopName
        };

        var commandLine = $"\"{exePath}\" {args}";

        if (CreateProcess(null, commandLine,
            IntPtr.Zero, IntPtr.Zero, false, 0,
            IntPtr.Zero, null, ref si, out var pi))
        {
            _focusProcessIds.Add(pi.dwProcessId);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
        }
    }

    public void Dispose() => ExitFocusDesktop();
}
