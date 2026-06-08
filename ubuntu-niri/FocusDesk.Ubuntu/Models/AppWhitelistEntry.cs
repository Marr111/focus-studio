namespace FocusDesk.Models;

public class AppWhitelistEntry
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
