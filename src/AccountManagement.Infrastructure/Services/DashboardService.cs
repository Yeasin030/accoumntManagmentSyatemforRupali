using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;
using AccountManagement.Domain.Services;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AccountDbContext _context;

    public DashboardService(AccountDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Today's stats
        var todayTransactions = await _context.Transactions
            .Where(t => t.TransactionDate == today && t.Status != TransactionStatus.Cancelled)
            .ToListAsync(cancellationToken);

        int todayCount = todayTransactions.Count;
        decimal todayRevenue = todayTransactions.Sum(t => t.TotalBillAmount);
        decimal todayDirectCosts = todayTransactions.Sum(t => t.TotalDirectCost);

        var todayPaymentAmounts = await _context.Payments
            .Where(p => p.PaymentDate == today)
            .Select(p => p.Amount)
            .ToListAsync(cancellationToken);
        decimal todayPayments = todayPaymentAmounts.Sum();

        var todayExpenseAmounts = await _context.Expenses
            .Where(e => e.ExpenseDate == today)
            .Select(e => e.Amount)
            .ToListAsync(cancellationToken);
        decimal todayExpenses = todayExpenseAmounts.Sum();

        // Month's stats
        var monthTransactions = await _context.Transactions
            .Where(t => t.TransactionDate >= firstDayOfMonth && t.TransactionDate <= lastDayOfMonth && t.Status != TransactionStatus.Cancelled)
            .ToListAsync(cancellationToken);

        decimal monthRevenue = monthTransactions.Sum(t => t.TotalBillAmount);
        decimal monthDirectCosts = monthTransactions.Sum(t => t.TotalDirectCost);

        var monthPaymentAmounts = await _context.Payments
            .Where(p => p.PaymentDate >= firstDayOfMonth && p.PaymentDate <= lastDayOfMonth)
            .Select(p => p.Amount)
            .ToListAsync(cancellationToken);
        decimal monthPayments = monthPaymentAmounts.Sum();

        var monthExpenseAmounts = await _context.Expenses
            .Where(e => e.ExpenseDate >= firstDayOfMonth && e.ExpenseDate <= lastDayOfMonth)
            .Select(e => e.Amount)
            .ToListAsync(cancellationToken);
        decimal monthExpenses = monthExpenseAmounts.Sum();

        decimal monthGrossProfit = FinancialCalculator.CalculateGrossProfit(monthRevenue, monthDirectCosts);
        decimal monthNetProfit = monthGrossProfit - monthExpenses;

        // Total Customer Due
        var customerBalances = await _context.Customers
            .Where(c => c.IsActive)
            .Select(c => c.CurrentBalance)
            .ToListAsync(cancellationToken);
        decimal totalDue = customerBalances.Sum();

        int activeCustomerCount = await _context.Customers
            .CountAsync(c => c.IsActive, cancellationToken);

        int totalMaterialCount = await _context.Materials
            .CountAsync(m => m.IsActive, cancellationToken);

        // Recent 10 Transactions
        var recent = await _context.Transactions
            .Include(t => t.Customer)
            .Include(t => t.Items)
            .Include(t => t.CostEntries)
            .Include(t => t.Payments)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                TransactionNumber = t.TransactionNumber,
                TransactionDate = t.TransactionDate,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer != null ? t.Customer.Name : string.Empty,
                WorkDescription = t.WorkDescription,
                TotalBillAmount = t.TotalBillAmount,
                TotalDirectCost = t.TotalDirectCost,
                TotalPaidAmount = t.TotalPaidAmount,
                BalanceDue = t.BalanceDue,
                Status = t.Status
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TodayTransactionCount = todayCount,
            TodayRevenue = todayRevenue,
            TodayDirectCosts = todayDirectCosts,
            TodayPaymentsReceived = todayPayments,
            TodayExpenses = todayExpenses,
            MonthRevenue = monthRevenue,
            MonthDirectCosts = monthDirectCosts,
            MonthPaymentsReceived = monthPayments,
            MonthExpenses = monthExpenses,
            MonthGrossProfit = monthGrossProfit,
            MonthNetProfit = monthNetProfit,
            TotalOutstandingCustomerDue = totalDue,
            ActiveCustomerCount = activeCustomerCount,
            TotalMaterialCount = totalMaterialCount,
            RecentTransactions = recent
        };
    }
}
