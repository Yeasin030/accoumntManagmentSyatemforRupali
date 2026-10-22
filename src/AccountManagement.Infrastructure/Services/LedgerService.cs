using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.Common.Models;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using AccountManagement.Domain.Services;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class LedgerService : ILedgerService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(AccountDbContext context, ILogger<LedgerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? customerId = null,
        TransactionStatus? status = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Customer)
            .Include(t => t.Items)
            .Include(t => t.CostEntries)
            .Include(t => t.Payments)
            .AsNoTracking()
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(t => t.TransactionNumber.ToLower().Contains(term) ||
                                     t.WorkDescription.ToLower().Contains(term) ||
                                     (t.Customer != null && t.Customer.Name.ToLower().Contains(term)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToDto(t))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Customer)
            .Include(t => t.Items)
            .Include(t => t.CostEntries)
            .Include(t => t.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return transaction == null ? null : MapToDto(transaction);
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {dto.CustomerId} was not found.");
        }

        string transactionNumber = await GenerateNextTransactionNumberAsync(dto.TransactionDate, cancellationToken);

        var transaction = new Transaction
        {
            TransactionNumber = transactionNumber,
            TransactionDate = dto.TransactionDate,
            CustomerId = dto.CustomerId,
            WorkDescription = dto.WorkDescription,
            Discount = dto.Discount,
            Notes = dto.Notes,
            Status = TransactionStatus.Active
        };

        // Add Items with Rate Snapshots
        foreach (var itemDto in dto.Items)
        {
            var item = new TransactionItem
            {
                MaterialId = itemDto.MaterialId,
                MaterialNameSnapshot = itemDto.MaterialName,
                GsmSnapshot = itemDto.Gsm,
                SizeSnapshot = itemDto.Size,
                Quantity = itemDto.Quantity,
                Unit = itemDto.Unit,
                UnitRateSnapshot = itemDto.UnitRate
            };
            item.CalculateCost();
            transaction.Items.Add(item);
        }

        // Add Production Cost Entries
        foreach (var costDto in dto.CostEntries)
        {
            transaction.CostEntries.Add(new CostEntry
            {
                Category = costDto.Category,
                Description = costDto.Description,
                Amount = costDto.Amount
            });
        }

        // Recalculate totals
        transaction.RecalculateTotals();

        // Handle optional initial advance payment
        if (dto.InitialPaymentAmount.HasValue && dto.InitialPaymentAmount.Value > 0)
        {
            string paymentNumber = $"PAY-{dto.TransactionDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var payment = new Payment
            {
                PaymentNumber = paymentNumber,
                PaymentDate = dto.TransactionDate,
                CustomerId = dto.CustomerId,
                Amount = dto.InitialPaymentAmount.Value,
                PaymentMethod = dto.InitialPaymentMethod ?? PaymentMethod.Cash,
                ReferenceNumber = dto.InitialPaymentReference,
                Remarks = $"Initial advance for job {transaction.TransactionNumber}"
            };

            transaction.Payments.Add(payment);
            transaction.RecalculateTotals();
        }

        // Update Customer Running Balance
        customer.CurrentBalance += transaction.BalanceDue;

        await _context.Transactions.AddAsync(transaction, cancellationToken);

        // Audit Log
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "CreateTransaction",
            EntityName = nameof(Transaction),
            EntityId = transaction.Id.ToString(),
            Details = $"Created transaction {transaction.TransactionNumber} with bill amount {transaction.TotalBillAmount} BDT",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created transaction {TransactionNumber} for customer {CustomerName}",
            transaction.TransactionNumber, customer.Name);

        return MapToDto(transaction);
    }

    public async Task<bool> CancelTransactionAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null)
            return false;

        if (transaction.Status == TransactionStatus.Cancelled)
            return true;

        decimal previousBalanceDue = transaction.BalanceDue;
        transaction.Cancel(reason);

        // Revert customer outstanding balance
        if (transaction.Customer != null)
        {
            transaction.Customer.CurrentBalance = Math.Max(0m, transaction.Customer.CurrentBalance - previousBalanceDue);
        }

        // Audit Log
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "CancelTransaction",
            EntityName = nameof(Transaction),
            EntityId = transaction.Id.ToString(),
            Details = $"Cancelled transaction {transaction.TransactionNumber}. Reason: {reason}",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Cancelled transaction {TransactionNumber}. Reason: {Reason}", transaction.TransactionNumber, reason);
        return true;
    }

    public async Task<string> GenerateNextTransactionNumberAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        string prefix = $"TXN-{date:yyyyMM}-";
        int count = await _context.Transactions
            .CountAsync(t => t.TransactionNumber.StartsWith(prefix), cancellationToken);

        return $"{prefix}{(count + 1):D4}";
    }

    private static TransactionDto MapToDto(Transaction t)
    {
        return new TransactionDto
        {
            Id = t.Id,
            TransactionNumber = t.TransactionNumber,
            TransactionDate = t.TransactionDate,
            CustomerId = t.CustomerId,
            CustomerName = t.Customer?.Name ?? string.Empty,
            CustomerPhone = t.Customer?.PhoneNumber,
            WorkDescription = t.WorkDescription,
            SubTotal = t.SubTotal,
            Discount = t.Discount,
            TotalBillAmount = t.TotalBillAmount,
            TotalDirectCost = t.TotalDirectCost,
            TotalPaidAmount = t.TotalPaidAmount,
            BalanceDue = t.BalanceDue,
            Status = t.Status,
            CancellationReason = t.CancellationReason,
            CancelledAt = t.CancelledAt,
            Notes = t.Notes,
            Items = t.Items.Select(i => new TransactionItemDto
            {
                Id = i.Id,
                MaterialId = i.MaterialId,
                MaterialName = i.MaterialNameSnapshot,
                Gsm = i.GsmSnapshot,
                Size = i.SizeSnapshot,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitRate = i.UnitRateSnapshot,
                TotalMaterialCost = i.TotalMaterialCost
            }).ToList(),
            CostEntries = t.CostEntries.Select(c => new CostEntryDto
            {
                Id = c.Id,
                Category = c.Category,
                Description = c.Description,
                Amount = c.Amount
            }).ToList(),
            Payments = t.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate,
                CustomerId = p.CustomerId,
                CustomerName = t.Customer?.Name ?? string.Empty,
                TransactionId = p.TransactionId,
                TransactionNumber = t.TransactionNumber,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                Remarks = p.Remarks
            }).ToList()
        };
    }
}
