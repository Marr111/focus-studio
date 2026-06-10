using FocusDesk.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;


namespace FocusDesk.Data;

public class AppDbContext : DbContext
{
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<TaskItem> TaskItems { get; set; } = null!;
    public DbSet<AppWhitelistEntry> WhitelistEntries { get; set; } = null!;
    public DbSet<BlockedSite> BlockedSites { get; set; } = null!;

    public AppDbContext() {}
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusDesk",
        "focusdesk.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            options.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>()
            .HasOne(s => s.TaskItem)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TaskItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed siti bloccati di default
        modelBuilder.Entity<BlockedSite>().HasData(
            new BlockedSite { Id = 1, Domain = "facebook.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 2, Domain = "instagram.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 3, Domain = "twitter.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 4, Domain = "x.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 5, Domain = "tiktok.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 6, Domain = "youtube.com", Category = "Video", IsEnabled = false },
            new BlockedSite { Id = 7, Domain = "reddit.com", Category = "News", IsEnabled = true },
            new BlockedSite { Id = 8, Domain = "twitch.tv", Category = "Video", IsEnabled = false }
        );
    }

    public override int SaveChanges()
    {
        var result = base.SaveChanges();
        _ = FocusDesk.Ubuntu.Services.GoogleDriveSyncService.UploadDbAsync();
        return result;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        _ = FocusDesk.Ubuntu.Services.GoogleDriveSyncService.UploadDbAsync();
        return result;
    }

    public override async System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        _ = FocusDesk.Ubuntu.Services.GoogleDriveSyncService.UploadDbAsync();
        return result;
    }

    public override async System.Threading.Tasks.Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, System.Threading.CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        _ = FocusDesk.Ubuntu.Services.GoogleDriveSyncService.UploadDbAsync();
        return result;
    }
}
