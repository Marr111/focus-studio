using FocusDesk.Models;
using FocusDesk.ViewModels;
using Avalonia.Controls; using Avalonia.Interactivity; using Avalonia;

using Avalonia.Input;
using Avalonia.Media;
using System.Windows.Input;
using Avalonia.VisualTree;

namespace FocusDesk.Views;

public partial class MainWindow : Window
{
    public MainViewModel MainVm { get; }
    public TasksViewModel TasksVm { get; }
    public StatsViewModel StatsVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public AgendaViewModel AgendaVm { get; }

    public void Close_Click(object? sender, RoutedEventArgs e) => Close();
    
    public void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    
    public void Maximize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private Button? _activeNavBtn;
    private readonly SolidColorBrush _activeNavColor;
    private readonly SolidColorBrush _inactiveNavColor;

    public MainWindow()
    {
        MainVm = new MainViewModel();
        TasksVm = new TasksViewModel(MainVm);
        AgendaVm = new AgendaViewModel(MainVm, TasksVm);
        StatsVm = new StatsViewModel();
        SettingsVm = new SettingsViewModel(MainVm);

        InitializeComponent();

        TasksTab.DataContext = TasksVm;
        AgendaTab.DataContext = AgendaVm;
        StatsTab.DataContext = StatsVm;
        SettingsTab.DataContext = SettingsVm;
        AIPlannerTab.DataContext = MainVm.AIPlannerVm;
        DataContext = MainVm;

        _activeNavColor = new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60));
        _inactiveNavColor = new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x80));

        // Imposta timer attivo di default e aggiorna progress ring
        SetActiveNav(NavTimer);
        MainVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainVm.Progress) || e.PropertyName == nameof(MainVm.CurrentMode))
                UpdateProgressRing(MainVm.Progress);
        };

        MainVm.RequestShowTimerTab += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ShowTab(TimerTab);
                SetActiveNav(NavTimer);
            });
        };
    }

    // ─── Drag finestra ────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
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
        
        ProgressRing.StrokeDashArray = new Avalonia.Collections.AvaloniaList<double>([dashOn, dashCircumference]);
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
        _ = MainVm.LoadDataAsync();
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

    private void NavAgenda_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(AgendaTab);
        SetActiveNav(NavAgenda);
        _ = AgendaVm.LoadSessionsAsync();
    }

    private void NavAIPlanner_Click(object sender, RoutedEventArgs e)
    {
        ShowTab(AIPlannerTab);
        SetActiveNav(NavAIPlanner);
        // _ = MainVm.AIPlannerVm.LoadExamsAsync(); can be called if needed, but it loads in ctor
    }

    private void ShowTab(Control target)
    {
        TimerTab.IsVisible = false;
        TasksTab.IsVisible = false;
        AgendaTab.IsVisible = false;
        StatsTab.IsVisible = false;
        SettingsTab.IsVisible = false;
        AIPlannerTab.IsVisible = false;
        target.IsVisible = true;
    }

    private void SetActiveNav(Button btn)
    {
        if (_activeNavBtn != null)
        {
            _activeNavBtn.Classes.Remove("Active");
        }

        _activeNavBtn = btn;
        if (btn != null && !btn.Classes.Contains("Active"))
        {
            btn.Classes.Add("Active");
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
