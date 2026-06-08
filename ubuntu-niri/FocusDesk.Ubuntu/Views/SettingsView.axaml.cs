

namespace FocusDesk.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    // Bottoni +/- per i valori numerici timer
    private void IncFocus_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.FocusDuration = Math.Min(vm.FocusDuration + 1, 120);
    }
    private void DecFocus_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.FocusDuration = Math.Max(vm.FocusDuration - 1, 1);
    }
    private void IncShort_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.ShortBreakDuration = Math.Min(vm.ShortBreakDuration + 1, 60);
    }
    private void DecShort_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.ShortBreakDuration = Math.Max(vm.ShortBreakDuration - 1, 1);
    }
    private void IncLong_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.LongBreakDuration = Math.Min(vm.LongBreakDuration + 1, 60);
    }
    private void DecLong_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.LongBreakDuration = Math.Max(vm.LongBreakDuration - 1, 1);
    }
    private void IncCycle_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.SessionsBeforeLongBreak = Math.Min(vm.SessionsBeforeLongBreak + 1, 10);
    }
    private void DecCycle_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            vm.SessionsBeforeLongBreak = Math.Max(vm.SessionsBeforeLongBreak - 1, 1);
    }
}
