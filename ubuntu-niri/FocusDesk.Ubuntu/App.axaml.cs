using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using FocusDesk.ViewModels;
using FocusDesk.Views;

namespace FocusDesk.Ubuntu;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        System.Threading.Tasks.Task.Run(async () => await FocusDesk.Ubuntu.Services.GoogleDriveSyncService.DownloadDbAsync()).Wait();

        using var db = new FocusDesk.Data.AppDbContext();
        db.Database.EnsureCreated();
        
        try
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, @"
                CREATE TABLE IF NOT EXISTS StudySessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Date TEXT NOT NULL,
                    Subject TEXT NOT NULL,
                    TimeOfDay INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    DurationHours REAL NOT NULL,
                    IsMappedToTask INTEGER NOT NULL DEFAULT 0,
                    TaskItemId INTEGER
                );
            ");
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, @"
                CREATE TABLE IF NOT EXISTS Exams (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    WrittenDate TEXT,
                    QuizDate TEXT,
                    OralDate TEXT,
                    PracticalDate TEXT,
                    Description TEXT
                );
            ");
            
            // Aggiunta dinamica delle colonne booleane per chi ha già la tabella
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "ALTER TABLE Exams ADD COLUMN HasWrittenExam INTEGER NOT NULL DEFAULT 0;"); } catch {}
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "ALTER TABLE Exams ADD COLUMN HasQuiz INTEGER NOT NULL DEFAULT 0;"); } catch {}
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "ALTER TABLE Exams ADD COLUMN HasOralExam INTEGER NOT NULL DEFAULT 0;"); } catch {}
            try { Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(db.Database, "ALTER TABLE Exams ADD COLUMN HasPracticalExam INTEGER NOT NULL DEFAULT 0;"); } catch {}
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error creating StudySessions table: {ex.Message}");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}