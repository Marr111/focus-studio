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
    public DbSet<StudySession> StudySessions { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<AppSettings> AppSettings { get; set; } = null!;

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

        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.Property(e => e.Volume).HasColumnType("real");
        });

        // Seed siti bloccati di default
        modelBuilder.Entity<BlockedSite>().HasData(
            new BlockedSite { Id = 1, Domain = "facebook.com", Category = "Social", IsEnabled = true },
            new BlockedSite { Id = 2, Domain = "instagram.com", Category = "Social", IsEnabled = true },
            new Blocked
