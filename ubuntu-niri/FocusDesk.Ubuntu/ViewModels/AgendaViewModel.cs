using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Data;
using FocusDesk.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace FocusDesk.ViewModels;

public enum AgendaViewMode
{
    Daily,
    Weekly,
    Monthly
}

public partial class AgendaViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly TasksViewModel _tasksVm;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    
    [ObservableProperty] private AgendaViewMode _viewMode = AgendaViewMode.Daily;
    
    [ObservableProperty] private string _dateDisplayText = string.Empty;
    
    [ObservableProperty] private string _newSessionSubject = string.Empty;
    [ObservableProperty] private string _newSessionTitle = string.Empty;
    [ObservableProperty] private string _newSessionDescription = string.Empty;
    [ObservableProperty] private double _newSessionDurationHours = 1.0;
    [ObservableProperty] private TimeOfDay _newSessionTimeOfDay = TimeOfDay.Mattina;

    public ObservableCollection<StudySession> Sessions { get; } = new();
    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = new();

    public AgendaViewModel(MainViewModel mainVm, TasksViewModel tasksVm)
    {
        _mainVm = mainVm;
        _tasksVm = tasksVm;
        _ = LoadSessionsAsync();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateDateDisplayAndLoadSessions();
    }

    partial void OnViewModeChanged(AgendaViewMode value)
    {
        OnPropertyChanged(nameof(IsDailyView));
        OnPropertyChanged(nameof(IsCalendarView));
        UpdateDateDisplayAndLoadSessions();
    }

    public bool IsDailyView => ViewMode == AgendaViewMode.Daily;
    public bool IsCalendarView => ViewMode != AgendaViewMode.Daily;

    private void UpdateDateDisplayAndLoadSessions()
    {
        switch (ViewMode)
        {
            case AgendaViewMode.Daily:
                DateDisplayText = SelectedDate.ToString("dd MMMM yyyy");
                break;
            case AgendaViewMode.Weekly:
                var startOfWeek = SelectedDate.AddDays(-(int)SelectedDate.DayOfWeek + (int)DayOfWeek.Monday);
                if (SelectedDate.DayOfWeek == DayOfWeek.Sunday) startOfWeek = SelectedDate.AddDays(-6);
                var endOfWeek = startOfWeek.AddDays(6);
                DateDisplayText = $"{startOfWeek:dd} - {endOfWeek:dd MMMM yyyy}";
                break;
            case AgendaViewMode.Monthly:
                DateDisplayText = SelectedDate.ToString("MMMM yyyy");
                break;
        }
        _ = LoadSessionsAsync();
    }

    [RelayCommand]
    private void PreviousPeriod()
    {
        SelectedDate = ViewMode switch
        {
            AgendaViewMode.Daily => SelectedDate.AddDays(-1),
            AgendaViewMode.Weekly => SelectedDate.AddDays(-7),
            AgendaViewMode.Monthly => SelectedDate.AddMonths(-1),
            _ => SelectedDate
        };
    }

    [RelayCommand]
    private void NextPeriod()
    {
        SelectedDate = ViewMode switch
        {
            AgendaViewMode.Daily => SelectedDate.AddDays(1),
            AgendaViewMode.Weekly => SelectedDate.AddDays(7),
            AgendaViewMode.Monthly => SelectedDate.AddMonths(1),
            _ => SelectedDate
        };
    }

    [RelayCommand]
    private void Today()
    {
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    private void SwitchToDay(DateTime date)
    {
        SelectedDate = date;
        ViewMode = AgendaViewMode.Daily;
    }

    [RelayCommand]
    private async Task AddSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSessionSubject) || string.IsNullOrWhiteSpace(NewSessionTitle))
            return;

        var session = new StudySession
        {
            Date = SelectedDate.Date,
            Subject = NewSessionSubject.Trim(),
            Title = NewSessionTitle.Trim(),
            Description = NewSessionDescription?.Trim(),
            DurationHours = NewSessionDurationHours,
            TimeOfDay = NewSessionTimeOfDay,
            IsMappedToTask = false
        };

        await using var db = new AppDbContext();
        db.StudySessions.Add(session);
        await db.SaveChangesAsync();

        NewSessionSubject = string.Empty;
        NewSessionTitle = string.Empty;
        NewSessionDescription = string.Empty;
        NewSessionDurationHours = 1.0;
        
        await LoadSessionsAsync();
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(StudySession session)
    {
        await using var db = new AppDbContext();
        var dbSession = await db.StudySessions.FindAsync(session.Id);
        if (dbSession != null)
        {
            db.StudySessions.Remove(dbSession);
            await db.SaveChangesAsync();
        }
        await LoadSessionsAsync();
    }

    [RelayCommand]
    private async Task StartAsTaskAsync(StudySession session)
    {
        if (session.IsMappedToTask && session.TaskItemId.HasValue)
        {
            // Se già mappata, prova a selezionarla e vai al timer
            var existingTask = _tasksVm.Tasks.FirstOrDefault(t => t.Id == session.TaskItemId.Value);
            if (existingTask != null)
            {
                _mainVm.SelectTaskCommand.Execute(existingTask);
                return;
            }
        }

        // Calcolo pomodori (1 ora = 2 pomodori da 25 min)
        int estimatedPomodoros = (int)Math.Ceiling(session.DurationHours * 2);
        if (estimatedPomodoros < 1) estimatedPomodoros = 1;

        var task = new TaskItem
        {
            Title = $"[{session.Subject}] {session.Title}",
            EstimatedPomodoros = estimatedPomodoros,
            CreatedAt = DateTime.Now,
            SortOrder = _tasksVm.Tasks.Count
        };

        await using var db = new AppDbContext();
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        // Aggiorna la sessione con l'ID del task creato
        var dbSession = await db.StudySessions.FindAsync(session.Id);
        if (dbSession != null)
        {
            dbSession.IsMappedToTask = true;
            dbSession.TaskItemId = task.Id;
            db.StudySessions.Update(dbSession);
            await db.SaveChangesAsync();
        }

        await _tasksVm.RefreshTasksAsync();
        
        // Seleziona il nuovo task e naviga (la UI di navigazione deve essere scatenata)
        // Aggiorna lo stato locale
        session.IsMappedToTask = true;
        session.TaskItemId = task.Id;
        
        var loadedTask = _tasksVm.Tasks.FirstOrDefault(t => t.Id == task.Id);
        if (loadedTask != null)
        {
            _mainVm.SelectTaskCommand.Execute(loadedTask);
        }
    }

    public async Task LoadSessionsAsync()
    {
        await using var db = new AppDbContext();
        var date = SelectedDate.Date;
        
        DateTime startDate = date;
        DateTime endDate = date;

        if (ViewMode == AgendaViewMode.Weekly)
        {
            startDate = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
            if (date.DayOfWeek == DayOfWeek.Sunday) startDate = date.AddDays(-6);
            endDate = startDate.AddDays(6);
        }
        else if (ViewMode == AgendaViewMode.Monthly)
        {
            startDate = new DateTime(date.Year, date.Month, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
        }

        var sessions = await db.StudySessions
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.TimeOfDay)
            .ThenBy(s => s.Id)
            .ToListAsync();

        Dispatcher.UIThread.Post(() =>
        {
            Sessions.Clear();
            CalendarDays.Clear();
            
            if (ViewMode == AgendaViewMode.Daily)
            {
                foreach (var s in sessions)
                {
                    Sessions.Add(s);
                }
            }
            else
            {
                // Genera la griglia dei giorni
                DateTime gridStart = startDate;
                DateTime gridEnd = endDate;
                
                if (ViewMode == AgendaViewMode.Monthly)
                {
                    // Per il mese, vogliamo mostrare una griglia 6x7 (42 giorni)
                    // Partiamo dal lunedì precedente all'inizio del mese
                    int diff = (int)gridStart.DayOfWeek - (int)DayOfWeek.Monday;
                    if (diff < 0) diff += 7; // se Domenica (0), diff = -1 -> 6
                    
                    gridStart = gridStart.AddDays(-diff);
                    gridEnd = gridStart.AddDays(41); // 42 giorni totali
                }
                
                for (var d = gridStart; d <= gridEnd; d = d.AddDays(1))
                {
                    bool isCurrentMonth = (ViewMode == AgendaViewMode.Weekly) || (d.Month == date.Month);
                    var dayItem = new CalendarDayItem(d, isCurrentMonth);
                    
                    var daySessions = sessions.Where(s => s.Date == d.Date);
                    foreach (var s in daySessions)
                    {
                        dayItem.Sessions.Add(s);
                    }
                    
                    CalendarDays.Add(dayItem);
                }
            }
        });
    }
}
