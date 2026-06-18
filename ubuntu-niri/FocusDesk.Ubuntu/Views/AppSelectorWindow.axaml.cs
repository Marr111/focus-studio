using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls; using Avalonia.Interactivity; using Avalonia;


namespace FocusDesk.Views;

public partial class AppSelectorWindow : Window
{
    public string? SelectedExecutable { get; private set; }
    public string? SelectedName { get; private set; }
    
    private List<AppItem> _allApps = new();

    public AppSelectorWindow()
    {
        InitializeComponent();
        LoadApps();
    }

    private async void LoadApps()
    {
        LoadingText.IsVisible = true;
        AppsList.IsVisible = false;
        SearchBox.IsVisible = false;
        
        var apps = await Task.Run(() =>
        {
            var list = new List<AppItem>();
            var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

            if (isLinux)
            {
                var desktopPaths = new List<string>
                {
                    "/usr/share/applications",
                    "/usr/local/share/applications",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications"),
                    "/var/lib/flatpak/exports/share/applications"
                };

                var seenExecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var dir in desktopPaths)
                {
                    if (!Directory.Exists(dir)) continue;

                    try
                    {
                        var files = Directory.GetFiles(dir, "*.desktop", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                var name = "";
                                var exec = "";
                                var noDisplay = false;
                                var isApp = true;
                                var inDesktopEntrySection = false;

                                foreach (var line in File.ReadLines(file))
                                {
                                    var trimmedLine = line.Trim();
                                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                                    {
                                        inDesktopEntrySection = trimmedLine.Equals("[Desktop Entry]", StringComparison.OrdinalIgnoreCase);
                                        continue;
                                    }

                                    if (!inDesktopEntrySection) continue;

                                    if (trimmedLine.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        name = trimmedLine.Substring(5).Trim();
                                    }
                                    else if (trimmedLine.StartsWith("Exec=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        exec = trimmedLine.Substring(5).Trim();
                                    }
                                    else if (trimmedLine.StartsWith("NoDisplay=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (bool.TryParse(trimmedLine.Substring(10).Trim(), out var nd))
                                        {
                                            noDisplay = nd;
                                        }
                                    }
                                    else if (trimmedLine.StartsWith("Type=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var typeVal = trimmedLine.Substring(5).Trim();
                                        if (!typeVal.Equals("Application", StringComparison.OrdinalIgnoreCase))
                                        {
                                            isApp = false;
                                        }
                                    }
                                }

                                if (!isApp || noDisplay || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(exec))
                                    continue;

                                // Clean exec line of placeholders
                                var placeholders = new string[] { "%u", "%U", "%f", "%F", "%i", "%c", "%k" };
                                foreach (var placeholder in placeholders)
                                {
                                    exec = exec.Replace(placeholder, "");
                                }
                                exec = exec.Replace("\"\"", "").Replace("''", "").Trim();

                                // If the exec starts/ends with quotes, remove them
                                if (exec.StartsWith("\"") && exec.EndsWith("\"") && exec.Length > 1)
                                {
                                    exec = exec.Substring(1, exec.Length - 2).Trim();
                                }

                                if (seenExecs.Contains(exec)) continue;
                                seenExecs.Add(exec);

                                list.Add(new AppItem
                                {
                                    Name = name,
                                    ExecutablePath = exec
                                });
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            else
            {
                var paths = new string[] { 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                };

                foreach (var basePath in paths.Distinct())
                {
                    if (!Directory.Exists(basePath)) continue;
                    
                    try
                    {
                        var exeFiles = GetFilesSafe(basePath, "*.exe");
                        foreach (var file in exeFiles)
                        {
                            var name = Path.GetFileNameWithoutExtension(file);
                            var lower = name.ToLowerInvariant();
                            if (lower.Contains("uninstall") || lower.Contains("unins") || lower == "update" || lower == "setup" || lower.Contains("crash")) 
                                continue;

                            list.Add(new AppItem
                            {
                                Name = name,
                                ExecutablePath = file
                            });
                        }
                    }
                    catch { }
                }
            }
            return list.OrderBy(a => a.Name).ToList();
        });

        _allApps = apps;
        AppsList.ItemsSource = _allApps;
        
        LoadingText.Text = $"Trovati {apps.Count} programmi.";
        AppsList.IsVisible = true;
        SearchBox.IsVisible = true;
    }

    private IEnumerable<string> GetFilesSafe(string path, string pattern)
    {
        var files = new List<string>();
        try
        {
            files.AddRange(Directory.EnumerateFiles(path, pattern));
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                files.AddRange(GetFilesSafe(directory, pattern));
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (PathTooLongException) { }
        catch (DirectoryNotFoundException) { }
        return files;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(query))
        {
            AppsList.ItemsSource = _allApps;
        }
        else
        {
            AppsList.ItemsSource = _allApps.Where(a => a.Name.ToLowerInvariant().Contains(query) || 
                                                       a.ExecutablePath.ToLowerInvariant().Contains(query));
        }
    }

    private void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        if (AppsList.SelectedItem is AppItem app)
        {
            SelectedExecutable = app.ExecutablePath;
            SelectedName = app.Name;
            Close(true);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

public class AppItem
{
    public string Name { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
}
