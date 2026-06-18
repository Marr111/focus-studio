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

public partial class AgendaViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly TasksViewModel _tasksVm;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    
    [ObservableProperty] private string _newSessionSubject = string.Empty;
    [ObservableProperty] private string _newSessionTitle = string.Empty;
    [ObservableProperty] private string _newSessionDescription = string.Empty;
    [ObservableProperty] private double _newSessionDurationHours = 1.0;
    [ObservableProperty] private TimeOfDay _newSessionTimeOfDay = TimeOfDay.Mattina;

    public ObservableCollection<StudySession> Sessions { get; } = new();

    public AgendaViewModel(MainViewModel mainVm, TasksViewModel tasksVm)
    {
        _mainVm = mainVm;
        _tasksVm = tasksVm;
        _ = LoadSessionsAsync();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadSessionsAsync();
    }

    [RelayCommand]
    private void PreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    private void NextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    private void Today()
    {
        SelectedDate = DateTime.Today;
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
        var sessions = await db.StudySessions
            .Where(s => s.Date.Date == date)
            .OrderBy(s => s.TimeOfDay)
            .ThenBy(s => s.Id)
            .ToListAsync();

        Dispatcher.UIThread.Post(() =>
        {
            Sessions.Clear();
            foreach (var s in sessions)
            {
                Sessions.Add(s);
            }
        });
    }
}
