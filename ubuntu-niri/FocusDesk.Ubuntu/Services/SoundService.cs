using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FocusDesk.Models;

namespace FocusDesk.Services;

public class SoundService : IDisposable
{
    private readonly PipeWirePlayer _tickingPlayer;
    private readonly PipeWirePlayer _alarmPlayer;
    private readonly PipeWirePlayer _uiPlayer;
    private readonly string _soundsDir;
    private readonly AppSettings _settings;

    private string _currentTickingPath = string.Empty;
    private bool _isTickingIntended;

    public bool IsTicking => _isTickingIntended;

    public SoundService(AppSettings settings)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _soundsDir = Path.Combine(baseDir, "Resources", "Sounds");

        _tickingPlayer = new PipeWirePlayer();
        _tickingPlayer.PlaybackFinished += OnTickingFinished;

        _alarmPlayer = new PipeWirePlayer();
        _uiPlayer = new PipeWirePlayer();

        _settings = settings;

        if (settings != null)
        {
            UpdateVolume((byte)settings.Volume);
        }
    }

    public void UpdateVolume(byte volume)
    {
        var volumeFactor = volume / 100.0;
        _tickingPlayer.Volume = volumeFactor;
        _alarmPlayer.Volume = volumeFactor;
        _uiPlayer.Volume = volumeFactor;

        // Se il ticchettio sta riproducendo, lo riavviamo al nuovo volume per feedback immediato
        if (_isTickingIntended && !string.IsNullOrEmpty(_currentTickingPath))
        {
            _tickingPlayer.Play(_currentTickingPath);
        }
    }

    private void OnTickingFinished(object? sender, EventArgs e)
    {
        if (_isTickingIntended && !string.IsNullOrEmpty(_currentTickingPath))
        {
            _tickingPlayer.Play(_currentTickingPath);
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
            _tickingPlayer.Play(path);
        }
    }

    public void StopTicking()
    {
        _isTickingIntended = false;
        _tickingPlayer.Stop();
    }

    public void PlayAlarm(AppSettings settings)
    {
        if (!settings.PlaySounds) return;
        var path = Path.Combine(_soundsDir, settings.SelectedAlarmSound);
        if (File.Exists(path))
        {
            _alarmPlayer.Play(path);
        }
    }

    public void PlayUiSound(AppSettings settings, string soundName = "button.wav")
    {
        if (!settings.EnableUiSounds) return;
        var path = Path.Combine(_soundsDir, soundName);
        if (File.Exists(path))
        {
            _uiPlayer.Play(path);
        }
    }

    public async Task PlayPreviewAsync(string soundName, int durationMs)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        var path = Path.Combine(_soundsDir, soundName);
        if (!File.Exists(path)) return;

        var previewPlayer = new PipeWirePlayer();
        previewPlayer.Volume = _settings != null ? _settings.Volume / 100.0 : 1.0;
        previewPlayer.Play(path);
        await Task.Delay(durationMs);
        previewPlayer.Stop();
    }

    public void Dispose()
    {
        _isTickingIntended = false;
        _tickingPlayer.Stop();
        _alarmPlayer.Stop();
        _uiPlayer.Stop();
    }
}

public class PipeWirePlayer
{
    private Process? _process;
    private double _volume = 1.0;
    
    public event EventHandler? PlaybackFinished;

    public double Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0, 1.0);
    }

    public void Play(string path)
    {
        Stop();

        try
        {
            var volumeArg = _volume.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            var psi = new ProcessStartInfo
            {
                FileName = "pw-play",
                Arguments = $"--volume {volumeArg} \"{path}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.Exited += (s, e) =>
            {
                var p = (Process?)s;
                p?.Dispose();
                if (_process == p)
                {
                    _process = null;
                    PlaybackFinished?.Invoke(this, EventArgs.Empty);
                }
            };

            _process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing with pw-play: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch {}
            _process.Dispose();
            _process = null;
        }
    }
}
