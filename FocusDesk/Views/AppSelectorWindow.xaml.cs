using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        LoadingText.Visibility = Visibility.Visible;
        AppsList.Visibility = Visibility.Collapsed;
        SearchBox.Visibility = Visibility.Collapsed;
        
        var apps = await Task.Run(() =>
        {
            var list = new List<AppItem>();
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
            return list.OrderBy(a => a.Name).ToList();
        });

        _allApps = apps;
        AppsList.ItemsSource = _allApps;
        
        LoadingText.Text = $"Trovati {apps.Count} programmi.";
        AppsList.Visibility = Visibility.Visible;
        SearchBox.Visibility = Visibility.Visible;
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
        var query = SearchBox.Text.ToLowerInvariant();
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
            DialogResult = true;
            Close();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class AppItem
{
    public string Name { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
}
