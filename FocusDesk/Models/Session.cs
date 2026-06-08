namespace FocusDesk.Models;

public enum SessionType
{
    Focus,
    PausaBreve,
    PausaLunga
}

public class Session
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionType Type { get; set; }
    public bool IsCompleted { get; set; }
    public int DurationMinutes { get; set; }
    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}
