using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Data;
using FocusDesk.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace FocusDesk.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    [ObservableProperty] private string _newTaskTitle = string.Empty;
    [ObservableProperty] private int _newTaskEstimatedPomodoros = 1;
    [ObservableProperty] private string _filterMode = "Tutte"; // "Tutte", "InCorso", "Completate"
    [ObservableProperty] private TaskItem? _selectedTask;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> FilteredTasks { get; } = new();

    private readonly MainViewModel _mainVm;

    public TasksViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        _ = LoadTasksAsync();
    }

    partial void OnFilterModeChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task AddTask()
    {
        var title = NewTaskTitle.Trim();
        if (string.IsNullOrEmpty(title)) return;

        var task = new TaskItem
        {
            Title = title,
            EstimatedPomodoros = NewTaskEstimatedPomodoros,
            CreatedAt = DateTime.Now,
            SortOrder = Tasks.Count
        };

        await using var db = new AppDbContext();
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        Tasks.Add(task);
        NewTaskTitle = string.Empty;
        NewTaskEstimatedPomodoros = 1;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ToggleComplete(TaskItem task)
    {
        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.Now : null;

        await using var db = new AppDbContext();
        db.TaskItems.Update(task);
        await db.SaveChangesAsync();

        // Se era il task selezionato nel timer, deselezionalo
        if (_mainVm.SelectedTask?.Id == task.Id && task.IsCompleted)
            _mainVm.SelectTaskCommand.Execute(null);

        ApplyFilter();
    }

    [RelayCommand]
    private async Task DeleteTask(TaskItem task)
    {
        Tasks.Remove(task);
        FilteredTasks.Remove(task);

        await using var db = new AppDbContext();
        var dbTask = await db.TaskItems.FindAsync(task.Id);
        if (dbTask != null)
        {
            db.TaskItems.Remove(dbTask);
            await db.SaveChangesAsync();
        }
    }

    [RelayCommand]
    private void SelectForTimer(TaskItem task)
    {
        SelectedTask = task;
        _mainVm.SelectTaskCommand.Execute(task);
    }

    [RelayCommand]
    private void IncrementEstimated()
    {
        NewTaskEstimatedPomodoros = Math.Min(NewTaskEstimatedPomodoros + 1, 20);
    }

    [RelayCommand]
    private void DecrementEstimated()
    {
        NewTaskEstimatedPomodoros = Math.Max(NewTaskEstimatedPomodoros - 1, 1);
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        FilterMode = filter;
    }

    public async Task RefreshTasksAsync() => await LoadTasksAsync();

    private async Task LoadTasksAsync()
    {
        await using var db = new AppDbContext();
        var tasks = await db.TaskItems
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        Dispatcher.UIThread.Post(() =>
        {
            Tasks.Clear();
            foreach (var t in tasks) Tasks.Add(t);
            ApplyFilter();
        });
    }

    private void ApplyFilter()
    {
        FilteredTasks.Clear();
        var filtered = FilterMode switch
        {
            "InCorso" => Tasks.Where(t => !t.IsCompleted),
            "Completate" => Tasks.Where(t => t.IsCompleted),
            _ => Tasks.AsEnumerable()
        };
        foreach (var t in filtered) FilteredTasks.Add(t);
    }

}
