using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FocusDesk.Services;

public static class GoogleDriveSyncService
{
    private static readonly string FolderId = "1iFawY-Z1Yaov-dLrAarFECGov8Eqxgh2";
    private static readonly string RemoteName = "gdrive";
    
    private static string DbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusDesk",
        "focusdesk.db");

    private static string DbDirectory => Path.GetDirectoryName(DbPath)!;

    private static Task<string> RunRcloneAsync(string arguments)
    {
        var tcs = new TaskCompletionSource<string>();
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rclone",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            process.Exited += async (sender, args) =>
            {
                try
                {
                    string output = await outputTask;
                    string error = await errorTask;
                    
                    if (process.ExitCode == 0)
                        tcs.SetResult(output);
                    else
                        tcs.SetException(new Exception($"Rclone failed with exit code {process.ExitCode}: {error}"));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    process.Dispose();
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start rclone: {ex.Message}");
            tcs.SetResult("");
        }
        return tcs.Task;
    }

    public static async Task<bool> DownloadDbAsync()
    {
        try
        {
            Directory.CreateDirectory(DbDirectory);
            
            // Check if drive is empty
            string lsOutput = "";
            try {
                lsOutput = await RunRcloneAsync($"ls {RemoteName}: --drive-root-folder-id {FolderId} --include \"focusdesk.db\"");
            } catch (Exception ex) {
                Console.WriteLine($"Sync check error: {ex.Message}");
                // If this fails (e.g. rclone not configured), we just abort gracefully
                return false;
            }

            if (string.IsNullOrWhiteSpace(lsOutput))
            {
                // Drive is empty. If local DB exists, let's upload it to initialize Drive
                if (File.Exists(DbPath))
                {
                    await UploadDbAsync();
                }
            }
            else
            {
                // Drive has data, download it (this overwrites local file with Drive's latest)
                string args = $"copy {RemoteName}: \"{DbDirectory}\" --drive-root-folder-id {FolderId} --include \"focusdesk.db\"";
                await RunRcloneAsync(args);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync download error: {ex.Message}");
            return false;
        }
    }

    private static CancellationTokenSource? _debounceCts;

    public static void DebounceUploadDbAsync(int delayMilliseconds = 2000)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMilliseconds, token);
                await UploadDbAsync();
            }
            catch (TaskCanceledException)
            {
                // Debounced
            }
        });
    }

    public static async Task UploadDbAsync()
    {
        try
        {
            if (!File.Exists(DbPath)) return;
            
            // Copy local file to drive
            string args = $"copy \"{DbPath}\" {RemoteName}: --drive-root-folder-id {FolderId}";
            await RunRcloneAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync upload error: {ex.Message}");
        }
    }
}
