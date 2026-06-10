using FocusDesk.Data;
using FocusDesk.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls; using Avalonia.Interactivity; using Avalonia;

using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
namespace FocusDesk.Views;

public partial class FocusOverlay : Window
{
    private readonly TimerService _timerService;

    private static readonly string[] Quotes =
    [
        "\"Il segreto per andare avanti è iniziare.\" — Mark Twain",
        "\"La concentrazione è la radice di tutte le capacità umane.\" — Bruce Lee",
        "\"Fai una cosa alla volta e falla bene.\" — Steve Jobs",
        "\"Il momento per rilassarsi è quando non hai tempo.\" — Sydney J. Harris",
        "\"La produttività non riguarda il fare di più; riguarda il fare le cose giuste.\"",
        "\"Ogni grande opera inizia con la decisione di provarci.\"",
        "\"Il tuo futuro è creato da quello che fai oggi, non domani.\""
    ];

    private Action? _onExit;

    public FocusOverlay()
    {
        InitializeComponent();
        _timerService = null!;
    }

    public FocusOverlay(TimerService timerService, Action? onExit = null)
    {
        InitializeComponent();
        _timerService = timerService;
        _onExit = onExit;

        // Citazione casuale
        MotivationText.Text = Quotes[new Random().Next(Quotes.Length)];

        // Timer update
        _timerService.Tick += OnTimerTick;
        _timerService.Completed += OnTimerCompleted;
        UpdateDisplay(_timerService.Remaining);

        // Carica app whitelist nel dock
        _ = LoadWhitelistAppsAsync();
    }

    private void OnTimerTick(object? sender, TimeSpan remaining)
    {
        UpdateDisplay(remaining);
    }

    private void OnTimerCompleted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => CloseOverlay());
    }

    private void UpdateDisplay(TimeSpan remaining)
    {
        TimerDisplay.Text = $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        double totalSeconds = _timerService.TotalDuration.TotalSeconds;
        double progress = totalSeconds > 0 ? remaining.TotalSeconds / totalSeconds : 0;
        ProgressBar.Width = Math.Max(0, 400 * progress);
    }

    private async Task LoadWhitelistAppsAsync()
    {
        await using var db = new AppDbContext();
        var apps = await db.WhitelistEntries.OrderBy(a => a.SortOrder).ToListAsync();

        Dispatcher.UIThread.Post(() =>
        {
            AppDock.Children.Clear();
            foreach (var app in apps)
            {
                var btn = CreateAppButton(app.DisplayName, app.ExecutablePath);
                AppDock.Children.Add(btn);
            }

            if (apps.Count == 0)
            {
                var hint = new TextBlock
                {
                    Text = "Nessuna app nella whitelist. Configurale nelle Impostazioni.",
                    Foreground = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
                    FontSize = 13,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                AppDock.Children.Add(hint);
            }
        });
    }

    private static Button CreateAppButton(string name, string exePath)
    {
        string safeName = name ?? string.Empty;
        var btn = new Button
        {
            Margin = new Thickness(8, 0, 8, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(btn, safeName);

        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical };

        // Icona app (usa una ellisse colorata come placeholder)
        var iconBorder = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)),
            Child = new TextBlock
            {
                Text = safeName.Length > 0 ? safeName[0].ToString().ToUpper() : "?",
                FontSize = 24,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };

        var label = new TextBlock
        {
            Text = safeName.Length > 12 ? safeName[..12] + "\u2026" : safeName,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        panel.Children.Add(iconBorder);
        panel.Children.Add(label);
        btn.Content = panel;

        btn.Click += (_, _) =>
        {
            try
            {
                string targetPath = exePath;
                if (targetPath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = System.IO.Path.GetFileName(exePath);
                }

                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(currentExe) && string.Equals(exePath, currentExe, StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                        if (mainWindow != null)
                        {
                            if (mainWindow.WindowState == WindowState.Minimized)
                                mainWindow.WindowState = WindowState.Normal;
                            mainWindow.Show();
                            mainWindow.Activate();
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            mainWindow.Focus();
                        }
                    });
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = targetPath,
                    UseShellExecute = true
                };

                if (targetPath.EndsWith("chrome.exe", StringComparison.OrdinalIgnoreCase) || 
                    targetPath.EndsWith("brave.exe", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.Arguments = "\"https://www.polito.it\" \"https://gemini.google.com\"";
                }

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _ = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Errore", $"Impossibile avviare {name}: {ex.Message}").ShowAsync();
            }
        };

        // Hover effect
        btn.PointerEntered += (_, _) =>
        {
            if (iconBorder.Child is TextBlock)
                iconBorder.Background = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
        };
        btn.PointerExited += (_, _) =>
        {
            iconBorder.Background = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
        };

        return btn;
    }

    private void ExitFocusMode_Click(object sender, RoutedEventArgs e)
    {
        CloseOverlay();
    }

    private void CloseOverlay()
    {
        _timerService.Tick -= OnTimerTick;
        _timerService.Completed -= OnTimerCompleted;
        _onExit?.Invoke();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timerService.Tick -= OnTimerTick;
        _timerService.Completed -= OnTimerCompleted;
        base.OnClosed(e);
    }
}
