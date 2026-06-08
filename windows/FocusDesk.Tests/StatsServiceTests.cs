using FocusDesk.Data;
using FocusDesk.Models;
using FocusDesk.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FocusDesk.Tests
{
    public class StatsServiceTests
    {
        private AppDbContext GetMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetTodayMinutesAsync_ShouldReturnCorrectMinutes()
        {
            using var db = GetMemoryContext();
            var service = new StatsService(() => db);
            
            db.Sessions.Add(new Session { StartTime = DateTime.Now, DurationMinutes = 25, IsCompleted = true, Type = SessionType.Focus });
            db.Sessions.Add(new Session { StartTime = DateTime.Now, DurationMinutes = 15, IsCompleted = true, Type = SessionType.Focus });
            db.Sessions.Add(new Session { StartTime = DateTime.Now.AddDays(-1), DurationMinutes = 30, IsCompleted = true, Type = SessionType.Focus });
            await db.SaveChangesAsync();

            var minutes = await service.GetTodayMinutesAsync();
            Assert.Equal(40, minutes);
        }
    }
}
