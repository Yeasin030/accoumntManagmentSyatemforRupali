using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AccountDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(
        Guid? customerId = null,
        Guid? transactionId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Transaction)
            .AsNoTracking()
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        if (transactionId.HasValue)
            query = query.Where(p => p.TransactionId == transactionId.Value);

        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                PaymentDate = p.PaymentDate,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null ? p.Customer.Name : string.Empty,
                TransactionId = p.TransactionId,
                TransactionNumber = p.Transaction != null ? p.Transaction.TransactionNumber : null,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                Remarks = p.Remarks
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentDto> RecordPaymentAsync(CreatePaymentDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer {dto.CustomerId} was not found.");

        string paymentNumber = $"PAY-{dto.PaymentDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            PaymentDate = dto.PaymentDate,
            CustomerId = dto.CustomerId,
            TransactionId = dto.TransactionId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber,
            Remarks = dto.Remarks
        };

        // Reduce Customer's current balance
        customer.CurrentBalance = Math.Max(0m, customer.CurrentBalance - dto.Amount);

        await _context.Payments.AddAsync(payment, cancellationToken);

        // If payment is assigned to a specific transaction, recalculate transaction paid amount and due
        if (dto.TransactionId.HasValue)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Payments)
                .Include(t => t.Items)
                .Include(t => t.CostEntries)
                .FirstOrDefaultAsync(t => t.Id == dto.TransactionId.Value, cancellationToken);

            if (transaction != null)
            {
                if (!transaction.Payments.Contains(payment))
                {
                    transaction.Payments.Add(payment);
                }
                transaction.RecalculateTotals();
            }
        }

        // Audit Log
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "RecordPayment",
            EntityName = nameof(Payment),
            EntityId = payment.Id.ToString(),
            Details = $"Recorded payment {payment.PaymentNumber} of {payment.Amount} BDT for customer {customer.Name}",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded payment {PaymentNumber} for customer {CustomerName}", payment.PaymentNumber, customer.Name);

        return new PaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            PaymentDate = payment.PaymentDate,
            CustomerId = payment.CustomerId,
            CustomerName = customer.Name,
            TransactionId = payment.TransactionId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Remarks = payment.Remarks
        };
    }

    public async Task<bool> DeletePaymentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Transaction)
                .ThenInclude(t => t!.Payments)
            .Include(p => p.Transaction)
                .ThenInclude(t => t!.Items)
            .Include(p => p.Transaction)
                .ThenInclude(t => t!.CostEntries)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment == null)
            return false;

        // Revert customer's current balance (customer owes that amount again)
        var customer = payment.Customer ?? await _context.Customers.FindAsync(new object[] { payment.CustomerId }, cancellationToken);
        if (customer != null)
        {
            customer.CurrentBalance += payment.Amount;
        }

        // If this payment was tied to a transaction, remove it and recalculate transaction totals
        if (payment.Transaction != null)
        {
            payment.Transaction.Payments.Remove(payment);
            payment.Transaction.RecalculateTotals();
        }

        _context.Payments.Remove(payment);

        // Audit Log
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Action = "DeletePayment",
            EntityName = nameof(Payment),
            EntityId = payment.Id.ToString(),
            Details = $"Deleted payment {payment.PaymentNumber} of {payment.Amount} BDT for customer {customer?.Name ?? "Unknown"}",
            ChangedBy = "Owner"
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Deleted payment {PaymentNumber} of {Amount} BDT for customer {CustomerName}",
            payment.PaymentNumber, payment.Amount, customer?.Name);

        return true;
    }
}
