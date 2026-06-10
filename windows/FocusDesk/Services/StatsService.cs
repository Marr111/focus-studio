using FocusDesk.Data;
using FocusDesk.Models;
using Microsoft.EntityFrameworkCore;

namespace FocusDesk.Services;

public record DailyStats(DateTime Date, int Sessions, int Minutes);

public record WeeklyStats(string Label, int Sessions);

public record HourlyStats(string Label, int Count);

public class StatsService
{
    private readonly Func<AppDbContext> _dbFactory;

    public StatsService()
    {
        _dbFactory = () => new AppDbContext();
    }

    public StatsService(Func<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task SaveSessionAsync(Session session)
    {
        await using var db = _dbFactory();
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
    }

    public async Task<int> GetTodaySessionsAsync()
    {
        await using var db = _dbFactory();
        return await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime.Date == DateTime.Today)
            .CountAsync();
    }

    public async Task<int> GetWeekSessionsAsync()
    {
        await using var db = _dbFactory();
        var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        return await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime >= startOfWeek)
            .CountAsync();
    }

    public async Task<int> GetMonthSessionsAsync()
    {
        await using var db = _dbFactory();
        var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        return await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime >= startOfMonth)
            .CountAsync();
    }

    public async Task<int> GetTotalSessionsAsync()
    {
        await using var db = _dbFactory();
        return await db.Sessions
            .Where(s => s.IsCompleted && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale))
            .CountAsync();
    }

    public async Task<int> GetStreakDaysAsync()
    {
        await using var db = _dbFactory();
        var dates = await db.Sessions
            .Where(s => s.IsCompleted && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale))
            .Select(s => s.StartTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        if (dates.Count == 0) return 0;

        int streak = 0;
        var expected = DateTime.Today;

        foreach (var date in dates)
        {
            if (date == expected)
            {
                streak++;
                expected = expected.AddDays(-1);
            }
            else if (date == expected.AddDays(-1) && streak == 0)
            {
                // Streak starts from yesterday if no sessions today
                streak++;
                expected = expected.AddDays(-2);
            }
            else break;
        }

        return streak;
    }

    public async Task<List<DailyStats>> GetLast30DaysAsync()
    {
        await using var db = _dbFactory();
        var startDate = DateTime.Today.AddDays(-29);

        var dbStats = await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime.Date >= startDate)
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new { Date = g.Key, Sessions = g.Count(), Minutes = g.Sum(s => s.DurationMinutes) })
            .ToListAsync();

        return Enumerable.Range(0, 30)
            .Select(i => startDate.AddDays(i))
            .Select(d =>
            {
                var found = dbStats.FirstOrDefault(x => x.Date == d);
                return new DailyStats(d, found?.Sessions ?? 0, found?.Minutes ?? 0);
            })
            .ToList();
    }

    public async Task<List<WeeklyStats>> GetLast7DaysAsync()
    {
        await using var db = _dbFactory();
        var startDate = DateTime.Today.AddDays(-6);

        var dbStats = await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime.Date >= startDate)
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        string[] dayNames = ["Lun", "Mar", "Mer", "Gio", "Ven", "Sab", "Dom"];

        return Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i))
            .Select(d =>
            {
                var count = dbStats.FirstOrDefault(x => x.Date == d)?.Count ?? 0;
                var dayName = dayNames[((int)d.DayOfWeek + 6) % 7];
                return new WeeklyStats(dayName, count);
            })
            .ToList();
    }

    public async Task<List<HourlyStats>> GetHourlyDistributionAsync(int days = 30)
    {
        await using var db = _dbFactory();
        var startDate = DateTime.Today.AddDays(-days);

        var dbStats = await db.Sessions
            .Where(s => s.IsCompleted
                        && s.Type == SessionType.Focus
                        && s.StartTime.Date >= startDate)
            .GroupBy(s => s.StartTime.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var count = dbStats.FirstOrDefault(x => x.Hour == h)?.Count ?? 0;
                return new HourlyStats($"{h:00}:00", count);
            })
            .ToList();
    }

    public async Task<List<HourlyStats>> GetAllTimeHourlyDistributionAsync()
    {
        await using var db = _dbFactory();

        var dbStats = await db.Sessions
            .Where(s => s.IsCompleted
                        && s.Type == SessionType.Focus)
            .GroupBy(s => s.StartTime.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToListAsync();

        return Enumerable.Range(0, 24)
            .Select(h =>
            {
                var count = dbStats.FirstOrDefault(x => x.Hour == h)?.Count ?? 0;
                return new HourlyStats($"{h:00}:00", count);
            })
            .ToList();
    }

    public async Task<int> GetTodayMinutesAsync()
    {
        await using var db = _dbFactory();
        return await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale)
                        && s.StartTime.Date == DateTime.Today)
            .SumAsync(s => s.DurationMinutes);
    }

    public async Task AddManualSessionsAsync(int count, DateTime date, int durationMinutes = 25)
    {
        if (count <= 0) return;
        await using var db = _dbFactory();
        
        for (int i = 0; i < count; i++)
        {
            db.Sessions.Add(new Session
            {
                StartTime = date,
                EndTime = date.AddMinutes(durationMinutes),
                Type = SessionType.FocusManuale,
                IsCompleted = true,
                DurationMinutes = durationMinutes
            });
        }
        
        await db.SaveChangesAsync();
    }

    public async Task<List<Session>> GetAllFocusSessionsAsync()
    {
        await using var db = _dbFactory();
        return await db.Sessions
            .Where(s => s.IsCompleted
                        && (s.Type == SessionType.Focus || s.Type == SessionType.FocusManuale))
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        await using var db = _dbFactory();
        var session = await db.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            db.Sessions.Remove(session);
            await db.SaveChangesAsync();
        }
    }
}
