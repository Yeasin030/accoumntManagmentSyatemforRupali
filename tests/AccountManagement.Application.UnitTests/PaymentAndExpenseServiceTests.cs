using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using AccountManagement.Infrastructure.Persistence;
using AccountManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class PaymentAndExpenseServiceTests
{
    private AccountDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task DeletePaymentAsync_DeletesPayment_AndRestoresCustomerBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Sonar Bangla Press",
            OpeningBalance = 5000m,
            CurrentBalance = 5000m
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var paymentService = new PaymentService(context, NullLogger<PaymentService>.Instance);

        var dto = new CreatePaymentDto
        {
            PaymentDate = new DateOnly(2026, 10, 22),
            CustomerId = customer.Id,
            Amount = 2000m,
            PaymentMethod = PaymentMethod.bKash,
            ReferenceNumber = "TRX998877",
            Remarks = "Partial payment"
        };

        var recorded = await paymentService.RecordPaymentAsync(dto);
        var custAfterPayment = await context.Customers.FindAsync(customer.Id);
        custAfterPayment!.CurrentBalance.Should().Be(3000m);

        // Act
        bool result = await paymentService.DeletePaymentAsync(recorded.Id);

        // Assert
        result.Should().BeTrue();
        var deletedPayment = await context.Payments.FindAsync(recorded.Id);
        deletedPayment.Should().BeNull();

        // Customer's current balance should be restored back to 5000m
        var custAfterDelete = await context.Customers.FindAsync(customer.Id);
        custAfterDelete!.CurrentBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task DeletePaymentAsync_WhenAttachedToTransaction_RecalculatesTransactionDue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Meghna Offset",
            OpeningBalance = 0m,
            CurrentBalance = 0m
        };
        await context.Customers.AddAsync(customer);

        var transaction = new Transaction
        {
            TransactionNumber = "TXN-202610-0001",
            TransactionDate = new DateOnly(2026, 10, 22),
            CustomerId = customer.Id,
            WorkDescription = "Calendar 2027",
            SubTotal = 10000m,
            TotalBillAmount = 10000m,
            TotalPaidAmount = 0m,
            BalanceDue = 10000m,
            Status = TransactionStatus.Active
        };
        customer.CurrentBalance = 10000m;
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        var paymentService = new PaymentService(context, NullLogger<PaymentService>.Instance);

        var paymentDto = new CreatePaymentDto
        {
            PaymentDate = new DateOnly(2026, 10, 22),
            CustomerId = customer.Id,
            TransactionId = transaction.Id,
            Amount = 4000m,
            PaymentMethod = PaymentMethod.Cash
        };

        var recorded = await paymentService.RecordPaymentAsync(paymentDto);
        var txnAfterPay = await context.Transactions.Include(t => t.Payments).FirstOrDefaultAsync(t => t.Id == transaction.Id);
        txnAfterPay!.TotalPaidAmount.Should().Be(4000m);
        txnAfterPay.BalanceDue.Should().Be(6000m);

        // Act
        bool result = await paymentService.DeletePaymentAsync(recorded.Id);

        // Assert
        result.Should().BeTrue();
        var txnAfterDelete = await context.Transactions.Include(t => t.Payments).FirstOrDefaultAsync(t => t.Id == transaction.Id);
        txnAfterDelete!.TotalPaidAmount.Should().Be(0m);
        txnAfterDelete.BalanceDue.Should().Be(10000m);

        var custAfterDelete = await context.Customers.FindAsync(customer.Id);
        custAfterDelete!.CurrentBalance.Should().Be(10000m);
    }

    [Fact]
    public async Task DeletePaymentAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var paymentService = new PaymentService(context, NullLogger<PaymentService>.Instance);

        // Act
        bool result = await paymentService.DeletePaymentAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExpenseAsync_DeletesExpense_AndLogsAudit()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var expenseService = new ExpenseService(context, NullLogger<ExpenseService>.Instance);

        var dto = new CreateExpenseDto
        {
            ExpenseDate = new DateOnly(2026, 10, 22),
            Category = "Machine Repair",
            Description = "Offset Cylinder Roller Replacement",
            Amount = 3500m,
            PaymentMethod = PaymentMethod.Cash,
            PaidTo = "Master Technician Rafiq"
        };

        var recorded = await expenseService.RecordExpenseAsync(dto);
        var expInDb = await context.Expenses.FindAsync(recorded.Id);
        expInDb.Should().NotBeNull();

        // Act
        bool result = await expenseService.DeleteExpenseAsync(recorded.Id);

        // Assert
        result.Should().BeTrue();
        var deletedExp = await context.Expenses.FindAsync(recorded.Id);
        deletedExp.Should().BeNull();

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "DeleteExpense" && a.EntityId == recorded.Id.ToString());
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("Machine Repair");
    }

    [Fact]
    public async Task DeleteExpenseAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var expenseService = new ExpenseService(context, NullLogger<ExpenseService>.Instance);

        // Act
        bool result = await expenseService.DeleteExpenseAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}
