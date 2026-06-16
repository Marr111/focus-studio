using NetCoreAudio;
using System.IO;
using FocusDesk.Models;

namespace FocusDesk.Services;

public class SoundService : IDisposable
{
    private readonly Player _tickingPlayer;
    private readonly Player _alarmPlayer;
    private readonly Player _uiPlayer;
    private readonly string _soundsDir;
    private readonly AppSettings _settings;

    private string _currentTickingPath = string.Empty;
    private bool _isTickingIntended;

    public SoundService(AppSettings settings)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _soundsDir = Path.Combine(baseDir, "Resources", "Sounds");

        _tickingPlayer = new Player();
        _tickingPlayer.PlaybackFinished += OnTickingFinished;

        _alarmPlayer = new Player();
        _uiPlayer = new Player();

        _settings = settings;
    }

    private void OnTickingFinished(object? sender, EventArgs e)
    {
        if (_isTickingIntended && !string.IsNullOrEmpty(_currentTickingPath))
        {
            _ = _tickingPlayer.Play(_currentTickingPath);
        }
    }

    public void StartTicking(AppSettings settings)
    {
        if (!settings.PlaySounds) return;
        var path = Path.Combine(_soundsDir, settings.SelectedTickingSound);
        if (File.Exists(path))
        {
            _isTickingIntended = true;
            _currentTickingPath = path;
            _ = _tickingPlayer.Play(path);
        }
    }

    public void StopTicking()
    {
        _isTickingIntended = false;
        _ = _tickingPlayer.Stop();
    }

    public void PlayAlarm(AppSettings settings)
    {
        if (!settings.PlaySounds) return;
        var path = Path.Combine(_soundsDir, settings.SelectedAlarmSound);
        if (File.Exists(path))
        {
            _ = _alarmPlayer.Play(path);
        }
    }

    public void PlayUiSound(AppSettings settings, string soundName = "button.wav")
    {
        if (!settings.EnableUiSounds) return;
        var path = Path.Combine(_soundsDir, soundName);
        if (File.Exists(path))
        {
            _ = _uiPlayer.Play(path);
        }
    }

    public async Task PlayPreviewAsync(string soundName, int durationMs)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        var path = Path.Combine(_soundsDir, soundName);
        if (!File.Exists(path)) return;

        var previewPlayer = new Player();
        await previewPlayer.Play(path);
        await Task.Delay(durationMs);
        await previewPlayer.Stop();
    }

    public void Dispose()
    {
        _isTickingIntended = false;
        _ = _tickingPlayer.Stop();
        _ = _alarmPlayer.Stop();
        _ = _uiPlayer.Stop();
    }
}
