using Avalonia.Threading;
using FocusDesk.Models;

namespace FocusDesk.Services;

public enum TimerState { Stopped, Running, Paused }

public class TimerService
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _remaining;
    private TimerState _state = TimerState.Stopped;

    public event EventHandler<TimeSpan>? Tick;
    public event EventHandler? Completed;

    public TimeSpan Remaining => _remaining;
    public TimerState State => _state;
    public TimeSpan TotalDuration { get; private set; }

    public TimerService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTick;
    }

    public void Start(TimeSpan duration)
    {
        TotalDuration = duration;
        _remaining = duration;
        _state = TimerState.Running;
        _timer.Start();
        Tick?.Invoke(this, _remaining);
    }

    public void Stop()
    {
        _timer.Stop();
        _state = TimerState.Stopped;
    }

    public void Pause()
    {
        if (_state != TimerState.Running) return;
        _timer.Stop();
        _state = TimerState.Paused;
    }

    public void Resume()
    {
        if (_state != TimerState.Paused) return;
        _timer.Start();
        _state = TimerState.Running;
    }

    public void Reset()
    {
        _timer.Stop();
        _state = TimerState.Stopped;
        _remaining = TotalDuration;
        Tick?.Invoke(this, _remaining);
    }

    public void SetDuration(TimeSpan duration)
    {
        _timer.Stop();
        _state = TimerState.Stopped;
        TotalDuration = duration;
        _remaining = duration;
        Tick?.Invoke(this, _remaining);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
        Tick?.Invoke(this, _remaining);

        if (_remaining <= TimeSpan.Zero)
        {
            _timer.Stop();
            _state = TimerState.Stopped;
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}
