using AccountManagement.Application.Common.Models;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Application.Common.Interfaces;

public interface ILedgerService
{
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? customerId = null,
        TransactionStatus? status = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<TransactionDto?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto, CancellationToken cancellationToken = default);
    Task<bool> CancelTransactionAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<bool> DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> GenerateNextTransactionNumberAsync(DateOnly date, CancellationToken cancellationToken = default);
}

public interface IRateService
{
    Task<IReadOnlyList<RatePeriodDto>> GetRatePeriodsAsync(CancellationToken cancellationToken = default);
    Task<RatePeriodDto> CreateRatePeriodAsync(string name, int year, int month, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialRateDto>> GetRatesForPeriodAsync(Guid ratePeriodId, CancellationToken cancellationToken = default);
    Task<decimal?> ResolveRateAsync(Guid materialId, DateOnly transactionDate, string? gsm = null, string? size = null, decimal quantity = 1, CancellationToken cancellationToken = default);
    Task<MaterialRateDto> SaveRateAsync(CreateOrUpdateMaterialRateDto dto, CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteCustomerAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(Guid? customerId = null, Guid? transactionId = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeletePaymentAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(DateOnly? startDate = null, DateOnly? endDate = null, string? category = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto> RecordExpenseAsync(CreateExpenseDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteExpenseAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}

public interface IMemoService
{
    Task<string> GenerateTransactionMemoHtmlAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<string> GenerateCustomerStatementMemoHtmlAsync(Guid customerId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
    Task<string> SaveMemoHtmlToFileAsync(string htmlContent, string fileNamePrefix, CancellationToken cancellationToken = default);
}

