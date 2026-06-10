using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

using FocusDesk.Models;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace FocusDesk.ViewModels;

public record HeatmapDay(DateTime Date, int Sessions);

/// <summary>Rappresenta un giorno con le sue sessioni nella cronologia.</summary>
public class DayGroup : ObservableObject
{
    public string DateLabel { get; init; } = "";
    public ObservableCollection<SessionItem> Sessions { get; init; } = new();
}

/// <summary>Wrapper ViewModel per una singola sessione visualizzata nella cronologia.</summary>
public class SessionItem : ObservableObject
{
    public int Id { get; init; }
    public DateTime StartTime { get; init; }
    public SessionType Type { get; init; }
    public int DurationMinutes { get; init; }

    public string Icon => Type == SessionType.FocusManuale ? "✏️" : "🍅";
    public string TypeLabel => Type == SessionType.FocusManuale ? "Manuale" : "Focus";
    public string TimeLabel => StartTime.ToString("ddd dd MMM — HH:mm");
    public string DurationLabel => $"{DurationMinutes} min";
}

public partial class StatsViewModel : ObservableObject
{
    private readonly StatsService _statsService;

    // ─── Stat Cards ───────────────────────────────────────────────────────────
    [ObservableProperty] private int _todaySessions;
    [ObservableProperty] private int _weekSessions;
    [ObservableProperty] private int _monthSessions;
    [ObservableProperty] private int _totalSessions;
    [ObservableProperty] private int _streakDays;
    [ObservableProperty] private int _todayMinutes;

    // ─── Grafici ──────────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _weeklySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _weeklyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _weeklyYAxes = Array.Empty<Axis>();

    // ─── Grafici Orari ────────────────────────────────────────────────────────
    [ObservableProperty] private ISeries[] _hourlySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _hourlyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _hourlyYAxes = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _allTimeHourlySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _allTimeHourlyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _allTimeHourlyYAxes = Array.Empty<Axis>();

    // ─── Heatmap ──────────────────────────────────────────────────────────────
    [ObservableProperty] private List<HeatmapDay> _heatmapDays = new();

    // ─── Cronologia Sessioni ──────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<DayGroup> _sessionHistory = new();

    // ─── Aggiunta Manuale ─────────────────────────────────────────────────────
    [ObservableProperty] private int _manualSessionCount = 1;
    [ObservableProperty] private DateTimeOffset? _manualSessionDate = DateTimeOffset.Now;

    public StatsViewModel()
    {
        _statsService = new StatsService();
        _ = LoadStatsAsync();
    }

    [RelayCommand]
    public async Task Refresh() => await LoadStatsAsync();

    [RelayCommand]
    public async Task AddManualSessions()
    {
        if (ManualSessionCount > 0 && ManualSessionDate.HasValue)
        {
            var settings = new SettingsService().Load();
            await _statsService.AddManualSessionsAsync(ManualSessionCount, ManualSessionDate.Value.DateTime, settings.FocusDuration);
            ManualSessionCount = 1;
            ManualSessionDate = DateTimeOffset.Now;
            await LoadStatsAsync();
        }
    }

    [RelayCommand]
    public async Task DeleteSession(SessionItem? item)
    {
        if (item == null) return;

        // Su Ubuntu potremmo usare MsBox.Avalonia per la conferma se volessimo
        await _statsService.DeleteSessionAsync(item.Id);
        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        // Carica metriche
        TodaySessions = await _statsService.GetTodaySessionsAsync();
        WeekSessions = await _statsService.GetWeekSessionsAsync();
        MonthSessions = await _statsService.GetMonthSessionsAsync();
        TotalSessions = await _statsService.GetTotalSessionsAsync();
        StreakDays = await _statsService.GetStreakDaysAsync();
        TodayMinutes = await _statsService.GetTodayMinutesAsync();

        // Carica dati per grafico settimanale
        var weeklyData = await _statsService.GetLast7DaysAsync();
        var values = weeklyData.Select(d => (double)d.Sessions).ToArray();
        var labels = weeklyData.Select(d => d.Label).ToArray();

        WeeklySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = values,
                Fill = new SolidColorPaint(new SKColor(0xE9, 0x45, 0x60, 0xCC)),
                Stroke = null,
                MaxBarWidth = 32,
                Rx = 6, Ry = 6
            }
        };

        WeeklyXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                TicksPaint = null,
                SeparatorsPaint = null
            }
        };

        WeeklyYAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MinStep = 1,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                TicksPaint = null,
                SeparatorsPaint = new SolidColorPaint(new SKColor(0x30, 0x30, 0x50, 0x80))
            }
        };

        // Carica dati orari (30 giorni)
        var hourlyData = await _statsService.GetHourlyDistributionAsync(30);
        var hourlyValues = hourlyData.Select(d => (double)d.Count).ToArray();
        var hourlyLabels = hourlyData.Select(d => d.Label).ToArray();

        HourlySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = hourlyValues,
                Fill = new SolidColorPaint(new SKColor(0x4A, 0x90, 0xE2, 0xCC)), // Blu
                Stroke = null,
                MaxBarWidth = 24,
                Rx = 4, Ry = 4
            }
        };

        HourlyXAxes = new Axis[]
        {
            new Axis
            {
                Labels = hourlyLabels,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                LabelsRotation = 45,
                TicksPaint = null,
                SeparatorsPaint = null
            }
        };

        HourlyYAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MinStep = 1,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                TicksPaint = null,
                SeparatorsPaint = new SolidColorPaint(new SKColor(0x30, 0x30, 0x50, 0x80))
            }
        };

        // Carica dati orari (di sempre)
        var allTimeHourlyData = await _statsService.GetAllTimeHourlyDistributionAsync();
        var allTimeHourlyValues = allTimeHourlyData.Select(d => (double)d.Count).ToArray();
        var allTimeHourlyLabels = allTimeHourlyData.Select(d => d.Label).ToArray();

        AllTimeHourlySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = allTimeHourlyValues,
                Fill = new SolidColorPaint(new SKColor(0xF5, 0xA6, 0x23, 0xCC)), // Arancione
                Stroke = null,
                MaxBarWidth = 24,
                Rx = 4, Ry = 4
            }
        };

        AllTimeHourlyXAxes = new Axis[]
        {
            new Axis
            {
                Labels = allTimeHourlyLabels,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                LabelsRotation = 45,
                TicksPaint = null,
                SeparatorsPaint = null
            }
        };

        AllTimeHourlyYAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MinStep = 1,
                LabelsPaint = new SolidColorPaint(new SKColor(0xA0, 0xA0, 0xB0)),
                TicksPaint = null,
                SeparatorsPaint = new SolidColorPaint(new SKColor(0x30, 0x30, 0x50, 0x80))
            }
        };

        // Carica dati heatmap (30 giorni)
        var last30 = await _statsService.GetLast30DaysAsync();
        HeatmapDays = last30.Select(d => new HeatmapDay(d.Date, d.Sessions)).ToList();

        // ─── Carica cronologia sessioni raggruppate per giorno ───────────────
        var allSessions = await _statsService.GetAllFocusSessionsAsync();

        var grouped = allSessions
            .GroupBy(s => s.StartTime.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var date = g.Key;
                string dateTitle;
                if (date == DateTime.Today)
                    dateTitle = $"Oggi  ·  {g.Count()} 🍅";
                else if (date == DateTime.Today.AddDays(-1))
                    dateTitle = $"Ieri  ·  {g.Count()} 🍅";
                else
                    dateTitle = $"{date:dd MMM yyyy}  ·  {g.Count()} 🍅";

                return new DayGroup
                {
                    DateLabel = dateTitle,
                    Sessions = new ObservableCollection<SessionItem>(
                        g.OrderByDescending(s => s.StartTime)
                         .Select(s => new SessionItem
                         {
                             Id = s.Id,
                             StartTime = s.StartTime,
                             Type = s.Type,
                             DurationMinutes = s.DurationMinutes
                         }))
                };
            })
            .ToList();

        Dispatcher.UIThread.Post(() =>
        {
            SessionHistory.Clear();
            foreach (var wg in grouped)
                SessionHistory.Add(wg);
        });
    }
}
