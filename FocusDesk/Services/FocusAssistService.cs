using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusDesk.Services;

/// <summary>
/// Gestisce l'integrazione con Windows Focus Assist (Non disturbare).
/// Può leggere lo stato corrente e abilitare/disabilitare Focus Assist
/// durante le sessioni di concentrazione.
/// 
/// NOTA: L'abilitazione programmatica usa chiavi di registro non documentate
/// (funziona su Windows 10 2004+ e Windows 11). Potrebbe non funzionare
/// su versioni future di Windows. In quel caso usa il fallback di apertura
/// delle impostazioni.
/// </summary>
public class FocusAssistService
{
    #region Shell API

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out QueryUserNotificationState state);

    private enum QueryUserNotificationState
    {
        NotPresent = 1,
        Busy = 2,
        RunningD3DFullScreen = 3,
        PresentationMode = 4,
        AcceptsNotifications = 5,
        QuietTime = 6,  // Focus Assist attivo
        App = 7
    }

    #endregion

    // Percorso registro per Windows 10 Focus Assist (Quiet Hours)
    private const string QuietHoursKey =
        @"Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.immersive.quiet_hours\Current";

    // Percorso registro per Windows 11 Focus Sessions
    private const string FocusSessionsKey =
        @"Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.data.shell.focussession\Current";

    private bool _previousStateEnabled;
    private bool _weChangedState;

    /// <summary>
    /// Controlla se Focus Assist (Non disturbare) è attualmente attivo.
    /// </summary>
    public bool IsActive
    {
        get
        {
            try
            {
                var result = SHQueryUserNotificationState(out var state);
                if (result == 0) // S_OK
                    return state == QueryUserNotificationState.QuietTime;
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// Abilita Focus Assist (Non disturbare) per la sessione focus.
    /// Salva lo stato precedente per ripristinarlo alla fine.
    /// Restituisce true se è riuscito ad abilitarlo.
    /// </summary>
    public bool EnableForSession()
    {
        _previousStateEnabled = IsActive;
        _weChangedState = false;

        if (_previousStateEnabled)
            return true; // già attivo, niente da fare

        try
        {
            // Tenta Windows 11 Focus Sessions prima
            if (TrySetFocusAssistViaRegistry(FocusSessionsKey, enable: true))
            {
                _weChangedState = true;
                return true;
            }

            // Tenta Windows 10 Quiet Hours
            if (TrySetFocusAssistViaRegistry(QuietHoursKey, enable: true))
            {
                _weChangedState = true;
                return true;
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Ripristina lo stato precedente di Focus Assist.
    /// </summary>
    public void RestoreAfterSession()
    {
        if (!_weChangedState || _previousStateEnabled) return;

        try
        {
            TrySetFocusAssistViaRegistry(FocusSessionsKey, enable: false);
            TrySetFocusAssistViaRegistry(QuietHoursKey, enable: false);
        }
        catch { }

        _weChangedState = false;
    }

    /// <summary>
    /// Apre le impostazioni Focus Assist di Windows come fallback.
    /// </summary>
    public static void OpenFocusAssistSettings()
    {
        try
        {
            // Windows 11: ms-settings:quiethours
            // Windows 10: ms-settings:quiethours (stesso URI)
            Process.Start(new ProcessStartInfo("ms-settings:quiethours")
            {
                UseShellExecute = true
            });
        }
        catch { }
    }

    /// <summary>
    /// Tenta di modificare lo stato di Focus Assist via registro.
    /// Il valore Data è un blob binario; il byte di controllo è all'offset 24.
    /// Valore 0x00 = disabilitato, 0x02 = Alarms Only (o Priority per Win11).
    /// </summary>
    private static bool TrySetFocusAssistViaRegistry(string keyPath, bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            if (key == null) return false;

            var data = key.GetValue("Data") as byte[];
            if (data == null || data.Length < 25) return false;

            // Byte 24 controlla la modalità Focus Assist
            // 0x00 = OFF, 0x01 = Priority Only, 0x02 = Alarms Only
            var originalByte = data[24];
            data[24] = enable ? (byte)0x02 : (byte)0x00;

            key.SetValue("Data", data, RegistryValueKind.Binary);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
