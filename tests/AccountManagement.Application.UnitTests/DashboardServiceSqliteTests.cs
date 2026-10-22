using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using AccountManagement.Infrastructure.Persistence;
using AccountManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class DashboardServiceSqliteTests
{
    [Fact]
    public async Task GetDashboardSummaryAsync_WithSqliteDecimals_ExecutesSuccessfullyWithoutSumError()
    {
        string testDbPath = Path.Combine(Path.GetTempPath(), $"test_dash_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseSqlite($"Data Source={testDbPath}")
                .Options;

            using var context = new AccountDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var customer = new Customer
            {
                Name = "Test Client",
                OpeningBalance = 500m,
                CurrentBalance = 1500m,
                IsActive = true
            };
            await context.Customers.AddAsync(customer);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var payment = new Payment
            {
                PaymentNumber = "PAY-001",
                PaymentDate = today,
                CustomerId = customer.Id,
                Amount = 850.50m,
                PaymentMethod = PaymentMethod.Cash
            };
            await context.Payments.AddAsync(payment);

            var expense = new Expense
            {
                ExpenseNumber = "EXP-001",
                ExpenseDate = today,
                Category = "Factory Rent",
                Amount = 12000m,
                PaymentMethod = PaymentMethod.BankTransfer
            };
            await context.Expenses.AddAsync(expense);

            await context.SaveChangesAsync();

            var dashboardService = new DashboardService(context);
            var summary = await dashboardService.GetDashboardSummaryAsync();

            summary.Should().NotBeNull();
            summary.TodayPaymentsReceived.Should().Be(850.50m);
            summary.TodayExpenses.Should().Be(12000m);
            summary.TotalOutstandingCustomerDue.Should().Be(1500m);
            summary.ActiveCustomerCount.Should().Be(1);
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
