using System;

namespace FocusDesk.Models;

public enum TimeOfDay
{
    Mattina,
    Pomeriggio,
    Sera
}

public class StudySession
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TimeOfDay TimeOfDay { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double DurationHours { get; set; }
    public bool IsMappedToTask { get; set; }
    public int? TaskItemId { get; set; }
}
