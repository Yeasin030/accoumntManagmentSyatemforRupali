using AccountManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class AccountDbContextSqliteTests
{
    [Fact]
    public async Task EnsureCreatedAsync_WithSqlite_CreatesAndSeedsSuccessfully()
    {
        string testDbPath = Path.Combine(Path.GetTempPath(), $"test_rupali_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseSqlite($"Data Source={testDbPath}")
                .Options;

            using (var context = new AccountDbContext(options))
            {
                bool created = await context.Database.EnsureCreatedAsync();
                created.Should().BeTrue();

                var seeder = new DatabaseSeeder(context, NullLogger<DatabaseSeeder>.Instance);
                await seeder.SeedAsync();

                int catCount = await context.MaterialCategories.CountAsync();
                catCount.Should().Be(7);

                int matCount = await context.Materials.CountAsync();
                matCount.Should().BeGreaterThan(10);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(testDbPath))
            {
                File.Delete(testDbPath);
            }
        }
    }
}
