using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Media;
using FocusDesk.Models;

namespace FocusDesk.Services;

/// <summary>
/// Servizio per la riproduzione di suoni tramite MediaPlayer (supporta MP3).
/// </summary>
public class SoundService : IDisposable
{
    private readonly string _soundsDir;
    private MediaPlayer? _tickingPlayer;
    private MediaPlayer? _alarmPlayer;
    private MediaPlayer? _uiPlayer;
    private bool _isTicking;
    private double _volume = 1.0;

    public bool IsTicking => _isTicking;

    public SoundService()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _soundsDir = Path.Combine(baseDir, "Resources", "Sounds");

        Application.Current.Dispatcher.Invoke(() =>
        {
            _tickingPlayer = new MediaPlayer();
            // Loop continuo per il ticchettio
            _tickingPlayer.MediaEnded += (s, e) =>
            {
                _tickingPlayer.Position = TimeSpan.Zero;
                _tickingPlayer.Play();
            };

            _alarmPlayer = new MediaPlayer();
            _uiPlayer = new MediaPlayer();
        });
    }

    public void UpdateVolume(byte volume)
    {
        _volume = volume / 100.0;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_tickingPlayer != null) _tickingPlayer.Volume = _volume;
            if (_alarmPlayer != null) _alarmPlayer.Volume = _volume;
            if (_uiPlayer != null) _uiPlayer.Volume = _volume;
        });
    }

    public void StartTicking(AppSettings settings)
    {
        if (!settings.EnableTickingSound || string.IsNullOrEmpty(settings.SelectedTickingSound)) return;

        var path = Path.Combine(_soundsDir, settings.SelectedTickingSound);
        if (!File.Exists(path)) return;

        _isTicking = true;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_tickingPlayer != null)
            {
                _tickingPlayer.Open(new Uri(path));
                _tickingPlayer.Volume = _volume;
                _tickingPlayer.Play();
            }
        });
    }

    public void StopTicking()
    {
        _isTicking = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            _tickingPlayer?.Stop();
        });
    }

    public void PlayAlarm(AppSettings settings)
    {
        if (!settings.EnableAlarmSound || string.IsNullOrEmpty(settings.SelectedAlarmSound))
        {
            if (settings.EnableAlarmSound) SystemSounds.Asterisk.Play(); // Fallback
            return;
        }

        var path = Path.Combine(_soundsDir, settings.SelectedAlarmSound);
        if (!File.Exists(path))
        {
            SystemSounds.Asterisk.Play();
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_alarmPlayer != null)
            {
                _alarmPlayer.Open(new Uri(path));
                _alarmPlayer.Volume = _volume;
                _alarmPlayer.Play();
            }
        });
    }

    public void PlayUiSound(AppSettings settings, string soundName = "button.wav")
    {
        if (!settings.EnableUiSounds) return;

        var path = Path.Combine(_soundsDir, soundName);
        if (!File.Exists(path)) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_uiPlayer != null)
            {
                _uiPlayer.Open(new Uri(path));
                _uiPlayer.Volume = _volume;
                _uiPlayer.Play();
            }
        });
    }

    public async Task PlayPreviewAsync(string soundName, int durationMs)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        var path = Path.Combine(_soundsDir, soundName);
        if (!File.Exists(path)) return;

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var previewPlayer = new MediaPlayer();
            previewPlayer.Open(new Uri(path));
            previewPlayer.Volume = _volume;
            previewPlayer.Play();

            await Task.Delay(durationMs);

            previewPlayer.Stop();
            previewPlayer.Close();
        });
    }

    public void Dispose()
    {
        _isTicking = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            _tickingPlayer?.Close();
            _alarmPlayer?.Close();
            _uiPlayer?.Close();
        });
    }
}
