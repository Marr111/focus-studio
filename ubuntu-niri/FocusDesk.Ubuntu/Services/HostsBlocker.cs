using System.Diagnostics;
using System.IO;
using System.Text;

namespace FocusDesk.Services;

public class HostsBlocker
{
    private const string HostsFilePath = "/etc/hosts";
    private const string StartMarker = "# --- FOCUSDESK BLOCK START ---";
    private const string EndMarker = "# --- FOCUSDESK BLOCK END ---";

    public bool IsCurrentlyBlocking { get; private set; }

    public async Task BlockSitesAsync(IEnumerable<string> domains)
    {
        var blockContent = new StringBuilder();
        blockContent.AppendLine(StartMarker);
        foreach (var domain in domains)
        {
            var normalized = NormalizeDomain(domain);
            if (!string.IsNullOrEmpty(normalized))
            {
                blockContent.AppendLine($"127.0.0.1 {normalized}");
                blockContent.AppendLine($"127.0.0.1 www.{normalized}");
            }
        }
        blockContent.AppendLine(EndMarker);

        await ApplyHostsChangeAsync(blockContent.ToString(), blocking: true);
    }

    public async Task UnblockAllAsync()
    {
        await ApplyHostsChangeAsync("", blocking: false);
    }

    private async Task ApplyHostsChangeAsync(string newBlockContent, bool blocking)
    {
        var currentContent = File.Exists(HostsFilePath) ? await File.ReadAllTextAsync(HostsFilePath) : "";
        var cleanContent = RemoveExistingBlock(currentContent);
        
        var finalContent = cleanContent;
        if (blocking && !string.IsNullOrEmpty(newBlockContent))
        {
            finalContent = cleanContent.TrimEnd() + Environment.NewLine + Environment.NewLine + newBlockContent;
        }

        var tmpPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmpPath, finalContent);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pkexec",
            Arguments = $"cp {tmpPath} {HostsFilePath}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new UnauthorizedAccessException("Permessi negati o operazione annullata.");
        }

        File.Delete(tmpPath);
        IsCurrentlyBlocking = blocking;
    }

    private string RemoveExistingBlock(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        var startIndex = lines.FindIndex(l => l.Trim() == StartMarker);
        var endIndex = lines.FindIndex(l => l.Trim() == EndMarker);

        if (startIndex >= 0 && endIndex >= startIndex)
        {
            lines.RemoveRange(startIndex, endIndex - startIndex + 1);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string NormalizeDomain(string input)
    {
        input = input.Trim().ToLowerInvariant();
        if (input.StartsWith("http://")) input = input.Substring(7);
        if (input.StartsWith("https://")) input = input.Substring(8);
        if (input.StartsWith("www.")) input = input.Substring(4);
        var slashIndex = input.IndexOf('/');
        if (slashIndex >= 0) input = input.Substring(0, slashIndex);
        return input;
    }
}
