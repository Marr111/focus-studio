namespace FocusDesk.Models;

public class BlockedSite
{
    public int Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Category { get; set; } // "Social", "News", "Custom"
}
