namespace FocusDesk.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool IsCompleted { get; set; }
    public int EstimatedPomodoros { get; set; } = 1;
    public int CompletedPomodoros { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
    public List<Session> Sessions { get; set; } = new();
}
