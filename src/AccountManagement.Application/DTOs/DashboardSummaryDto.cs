namespace AccountManagement.Application.DTOs;

public class DashboardSummaryDto
{
    // Today
    public int TodayTransactionCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal TodayDirectCosts { get; set; }
    public decimal TodayPaymentsReceived { get; set; }
    public decimal TodayExpenses { get; set; }

    // This Month
    public decimal MonthRevenue { get; set; }
    public decimal MonthDirectCosts { get; set; }
    public decimal MonthPaymentsReceived { get; set; }
    public decimal MonthExpenses { get; set; }
    public decimal MonthGrossProfit { get; set; }
    public decimal MonthNetProfit { get; set; }

    // Overall
    public decimal TotalOutstandingCustomerDue { get; set; }
    public int ActiveCustomerCount { get; set; }
    public int TotalMaterialCount { get; set; }

    // Recent Transactions
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}
