using FocusDesk.Models;
using FocusDesk.ViewModels;
using Avalonia.Controls; using Avalonia.Interactivity; using Avalonia;

using Avalonia.Input;
using Avalonia.Media;

namespace FocusDesk.Views;

public partial class MainWindow : Window
{
    public MainViewModel MainVm { get; }
    public TasksViewModel TasksVm { get; }
    public StatsViewModel StatsVm { get; }
    public SettingsViewModel SettingsVm { get; }

    // Comandi finestra per la title bar
    public ICommand CloseCommand { get; } = new RelayCommandSimple(() => ((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow?.Close());
    public ICommand MinimizeCommand { get; } = new RelayCommandSimple(() => { if (((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow != null) ((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow.WindowState = WindowState.Minimized; });
    public ICommand MaximizeCommand { get; } = new RelayCommandSimple(() =>
    {
        if (((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow == null) return;
        ((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow.WindowState =
            ((Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    });

    private Button? _activeNavBtn;
    private readonly SolidColorBrush _activeNavColor;
    private readonly SolidColorBrush _inactiveNavColor;

    public MainWindow()
    {
        MainVm = new MainViewModel();
        TasksVm = new TasksViewModel(MainVm);
        StatsVm = new StatsViewModel();
        SettingsVm = new SettingsViewModel(MainVm);

        DataContext = this;

        InitializeComponent();

        _activeNavColor = new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60));
        _inactiveNavColor = new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x80));

        // Imposta timer attivo di default e aggiorna progress ring
        SetActiveNav(NavTimer);
        MainVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainVm.Progress) || e.PropertyName == nameof(MainVm.CurrentMode))
                UpdateProgressRing(MainVm.Progress);
        };
    }

    // ─── Drag finestra ────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, PointerPressedEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ─── Progress ring aggiornamento ──────────────────────────────────────────
    private void UpdateProgressRing(double progress)
    {
        // Raggio = (Width - StrokeThickness) / 2 = (220 - 10) / 2 = 105
        double radius = 105.0;
        double strokeThickness = 10.0;
        double circumference = 2 * Math.PI * radius;
        
        // StrokeDashArray values in WPF are multiples of StrokeThickness
        double dashCircumference = circumference / strokeThickness;
        double dashOn = dashCircumference * Math.Clamp(progress, 0, 1);
        
        ProgressRing.StrokeDashArray = new DoubleCollection([dashOn, dashCircumference]);
        ProgressRing.StrokeDashOffset = 0;
        ProgressRing.Stroke = MainVm.CurrentMode switch
        {
            Models.SessionType.Focus => new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60)),
            Models.SessionType.PausaBreve => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x91)),
            Models.SessionType.PausaLunga => new SolidColorBrush(Color.FromRgb(0x45, 0x7B, 0xBD)),
            _ => ProgressRing.Stroke
        };
    }

    // ─── Navigazione tab ──────────────────────────────────────────────────────
    private void NavTimer_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(TimerTab);
        SetActiveNav(NavTimer);
    }

    private void NavTasks_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(TasksTab);
        SetActiveNav(NavTasks);
        _ = TasksVm.RefreshTasksAsync();
    }

    private async void NavStats_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(StatsTab);
        SetActiveNav(NavStats);
        await StatsVm.RefreshCommand.ExecuteAsync(null);
    }

    private void NavSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(SettingsTab);
        SetActiveNav(NavSettings);
    }

    private void ShowTab(UIElement target)
    {
        TimerTab.Visibility = Visibility.Collapsed;
        TasksTab.Visibility = Visibility.Collapsed;
        StatsTab.Visibility = Visibility.Collapsed;
        SettingsTab.Visibility = Visibility.Collapsed;
        target.Visibility = Visibility.Visible;
    }

    private void SetActiveNav(Button btn)
    {
        if (_activeNavBtn != null)
            SetNavButtonColor(_activeNavBtn, _inactiveNavColor);

        _activeNavBtn = btn;
        SetNavButtonColor(btn, _activeNavColor);
    }

    private static void SetNavButtonColor(Button btn, SolidColorBrush brush)
    {
        foreach (var tb in FindVisualChildren<TextBlock>(btn))
            tb.Foreground = brush;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T typed) yield return typed;
            foreach (var c in FindVisualChildren<T>(child)) yield return c;
        }
    }
}

/// <summary>Semplice RelayCommand per i comandi della finestra</summary>
public class RelayCommandSimple : ICommand
{
    private readonly Action _execute;
    public RelayCommandSimple(Action execute) => _execute = execute;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}
