using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;


namespace FocusDesk.Services;

/// <summary>
/// Blocca siti distraenti modificando il file hosts di Windows.
/// Richiede privilegi di Amministratore (gestito dal manifest UAC).
/// </summary>
public class HostsBlocker
{
    private const string HostsPath = @"C:\Windows\System32\drivers\etc\hosts";
    private const string MarkerStart = "# === FocusDesk INIZIO BLOCCO ===";
    private const string MarkerEnd = "# === FocusDesk FINE BLOCCO ===";
    private const string BlockedIp = "127.0.0.1";

    // Regex per normalizzare un URL/dominio al solo hostname (es. "www.example.com" → "example.com")
    private static readonly Regex DomainNormalizer =
        new(@"^(https?://)?(www\.)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool IsCurrentlyBlocking { get; private set; }

    /// <summary>Normalizza un input utente (URL, dominio con/senza www) al dominio pulito.</summary>
    public static string NormalizeDomain(string input)
    {
        var cleaned = input.Trim().ToLowerInvariant();
        cleaned = DomainNormalizer.Replace(cleaned, "");
        // Rimuove path e query string se presenti
        var slashIdx = cleaned.IndexOf('/');
        if (slashIdx >= 0) cleaned = cleaned[..slashIdx];
        return cleaned.TrimEnd('.');
    }

    public async Task BlockSitesAsync(IEnumerable<string> domains)
    {
        var domainList = domains.Select(NormalizeDomain).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().ToList();
        if (domainList.Count == 0) return;

        var content = await ReadHostsAsync();
        content = RemoveExistingBlock(content);

        // Aggiunge sia la versione con www. che senza per ogni dominio
        var lines = domainList.SelectMany(d => new[]
        {
            $"{BlockedIp} {d}",
            $"{BlockedIp} www.{d}"
        }).Distinct();

        var block = $"\n{MarkerStart}\n{string.Join("\n", lines)}\n{MarkerEnd}\n";
        content += block;

        await WriteHostsAsync(content);
        await FlushDnsAsync();
        IsCurrentlyBlocking = true;
    }

    public async Task UnblockAllAsync()
    {
        var content = await ReadHostsAsync();
        content = RemoveExistingBlock(content);
        await WriteHostsAsync(content);
        await FlushDnsAsync();
        IsCurrentlyBlocking = false;
    }

    public async Task<bool> HasBlockedSitesAsync()
    {
        var content = await ReadHostsAsync();
        return content.Contains(MarkerStart);
    }

    private static string RemoveExistingBlock(string content)
    {
        var startIdx = content.IndexOf(MarkerStart, StringComparison.Ordinal);
        var endIdx = content.IndexOf(MarkerEnd, StringComparison.Ordinal);

        if (startIdx < 0 || endIdx < 0) return content;

        // Rimuovi tutto dal marker di inizio alla fine del marker di fine
        var endOfBlock = endIdx + MarkerEnd.Length;
        // Includi il newline successivo se presente
        if (endOfBlock < content.Length && content[endOfBlock] == '\n')
            endOfBlock++;

        return content.Remove(startIdx, endOfBlock - startIdx);
    }

    private static async Task<string> ReadHostsAsync()
    {
        return await File.ReadAllTextAsync(HostsPath);
    }

    private static async Task WriteHostsAsync(string content)
    {
        await File.WriteAllTextAsync(HostsPath, content);
    }

    private static async Task FlushDnsAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig.exe",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var proc = Process.Start(psi);
            if (proc != null) await proc.WaitForExitAsync();
        }
        catch
        {
            // Non critico se flushdns fallisce
        }
    }
}
