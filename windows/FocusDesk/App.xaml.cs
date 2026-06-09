using Microsoft.EntityFrameworkCore;
using FocusDesk.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Windows;

namespace FocusDesk;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configura LiveCharts
        LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .AddDefaultMappers()
                .AddDarkTheme());

        System.Threading.Tasks.Task.Run(async () => await FocusDesk.Services.GoogleDriveSyncService.DownloadDbAsync()).Wait();

        // Inizializza e migra il database
        using var db = new AppDbContext();
        db.Database.Migrate();

        var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(currentExe) && !db.WhitelistEntries.Any(e => e.ExecutablePath == currentExe))
        {
            db.WhitelistEntries.Add(new FocusDesk.Models.AppWhitelistEntry
            {
                DisplayName = "FocusDesk",
                ExecutablePath = currentExe,
                SortOrder = 0
            });
            db.SaveChanges();
        }
    }
}
