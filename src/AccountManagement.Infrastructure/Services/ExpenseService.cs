using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(AccountDbContext context, ILogger<ExpenseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetExpensesAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                ExpenseNumber = e.ExpenseNumber,
                ExpenseDate = e.ExpenseDate,
                Category = e.Category,
                Description = e.Description,
                Amount = e.Amount,
                PaymentMethod = e.PaymentMethod,
                ReferenceNumber = e.ReferenceNumber,
                PaidTo = e.PaidTo,
                Remarks = e.Remarks
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseDto> RecordExpenseAsync(CreateExpenseDto dto, CancellationToken cancellationToken = default)
    {
        string expenseNumber = $"EXP-{dto.ExpenseDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        var expense = new Expense
        {
            ExpenseNumber = expenseNumber,
            ExpenseDate = dto.ExpenseDate,
            Category = dto.Category,
            Description = dto.Description,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            PaidTo = dto.PaidTo,
            Remarks = dto.Remarks
        };

        await _context.Expenses.AddAsync(expense, cancellationToken);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "RecordExpense",
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Details = $"Recorded expense {expense.ExpenseNumber} for {expense.Category}: {expense.Amount} BDT",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded expense {ExpenseNumber}: {Amount} BDT", expense.ExpenseNumber, expense.Amount);

        return new ExpenseDto
        {
            Id = expense.Id,
            ExpenseNumber = expense.ExpenseNumber,
            ExpenseDate = expense.ExpenseDate,
            Category = expense.Category,
            Description = expense.Description,
            Amount = expense.Amount,
            PaymentMethod = expense.PaymentMethod,
            ReferenceNumber = expense.ReferenceNumber,
            PaidTo = expense.PaidTo,
            Remarks = expense.Remarks
        };
    }

    public async Task<bool> DeleteExpenseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (expense == null)
            return false;

        _context.Expenses.Remove(expense);

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "DeleteExpense",
            EntityName = nameof(Expense),
            EntityId = expense.Id.ToString(),
            Details = $"Deleted expense {expense.ExpenseNumber} ({expense.Category}: {expense.Amount} BDT)",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted expense {ExpenseNumber}: {Amount} BDT", expense.ExpenseNumber, expense.Amount);
        return true;
    }
}
