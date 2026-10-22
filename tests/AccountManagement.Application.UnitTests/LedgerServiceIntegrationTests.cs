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

public class LedgerServiceIntegrationTests
{
    private AccountDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task CreateTransactionAsync_CreatesTransaction_WithRateSnapshotsAndUpdatedCustomerBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Al-Madina Printers",
            PhoneNumber = "01711223344",
            OpeningBalance = 0m,
            CurrentBalance = 0m
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var service = new LedgerService(context, NullLogger<LedgerService>.Instance);

        var dto = new CreateTransactionDto
        {
            TransactionDate = new DateOnly(2026, 10, 21),
            CustomerId = customer.Id,
            WorkDescription = "5000 Cash Memo Booklets",
            Discount = 500m,
            Items = new List<CreateTransactionItemDto>
            {
                new()
                {
                    MaterialName = "Art Paper 80 GSM",
                    Gsm = "80 GSM",
                    Size = "23x36",
                    Quantity = 10m,
                    UnitRate = 1200m
                }
            },
            CostEntries = new List<CreateCostEntryDto>
            {
                new()
                {
                    Category = CostCategory.Plate,
                    Description = "4 Sets of Master Plates",
                    Amount = 1000m
                },
                new()
                {
                    Category = CostCategory.Printing,
                    Description = "Offset 2-Color Run",
                    Amount = 3000m
                }
            },
            InitialPaymentAmount = 5000m,
            InitialPaymentMethod = PaymentMethod.bKash,
            InitialPaymentReference = "TRX998877"
        };

        // Act
        var created = await service.CreateTransactionAsync(dto);

        // Assert
        // Paper cost = 10 * 1200 = 12,000
        // Production cost = 1000 + 3000 = 4000
        // SubTotal = 16,000
        // TotalBill = 16,000 - 500 = 15,500
        // Paid = 5,000
        // BalanceDue = 10,500
        created.Should().NotBeNull();
        created.SubTotal.Should().Be(16000m);
        created.TotalBillAmount.Should().Be(15500m);
        created.TotalPaidAmount.Should().Be(5000m);
        created.BalanceDue.Should().Be(10500m);
        created.Status.Should().Be(TransactionStatus.Active);
        created.Items.Should().HaveCount(1);
        created.Items[0].MaterialName.Should().Be("Art Paper 80 GSM");
        created.Items[0].UnitRate.Should().Be(1200m);

        // Verify Customer balance updated
        var updatedCustomer = await context.Customers.FindAsync(customer.Id);
        updatedCustomer!.CurrentBalance.Should().Be(10500m);
    }

    [Fact]
    public async Task CancelTransactionAsync_CancelsTransaction_AndRevertsCustomerBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Green Leaf Publishers",
            OpeningBalance = 0m,
            CurrentBalance = 0m
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var service = new LedgerService(context, NullLogger<LedgerService>.Instance);

        var dto = new CreateTransactionDto
        {
            TransactionDate = new DateOnly(2026, 10, 21),
            CustomerId = customer.Id,
            WorkDescription = "Cancelled order test",
            Discount = 0m,
            Items = new List<CreateTransactionItemDto>
            {
                new() { MaterialName = "Offset 80 GSM", Quantity = 5m, UnitRate = 1000m }
            }
        };

        var created = await service.CreateTransactionAsync(dto);
        var custAfterCreate = await context.Customers.FindAsync(customer.Id);
        custAfterCreate!.CurrentBalance.Should().Be(5000m);

        // Act
        bool result = await service.CancelTransactionAsync(created.Id, "Customer cancelled job before press setup");

        // Assert
        result.Should().BeTrue();
        var cancelledTxn = await context.Transactions.FindAsync(created.Id);
        cancelledTxn!.Status.Should().Be(TransactionStatus.Cancelled);
        cancelledTxn.CancellationReason.Should().Be("Customer cancelled job before press setup");

        var custAfterCancel = await context.Customers.FindAsync(customer.Id);
        custAfterCancel!.CurrentBalance.Should().Be(0m);
    }
}
