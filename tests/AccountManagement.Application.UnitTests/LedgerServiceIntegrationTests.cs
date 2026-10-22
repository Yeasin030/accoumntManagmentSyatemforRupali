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

    [Fact]
    public async Task DeleteTransactionAsync_DeletesTransaction_AndRevertsCustomerBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Padma Publications",
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
            WorkDescription = "Brochure Print 5000 pcs",
            Discount = 0m,
            InitialPaymentAmount = 1500m,
            Items = new List<CreateTransactionItemDto>
            {
                new() { MaterialName = "Art Card 300 GSM", Quantity = 10m, UnitRate = 500m } // 5000 total
            }
        };

        var created = await service.CreateTransactionAsync(dto);
        var custAfterCreate = await context.Customers.FindAsync(customer.Id);
        // Bill: 5000, Advance Paid: 1500, Due: 3500 -> Customer balance is 3500
        custAfterCreate!.CurrentBalance.Should().Be(3500m);

        // Act
        bool result = await service.DeleteTransactionAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var deletedTxn = await context.Transactions.FindAsync(created.Id);
        deletedTxn.Should().BeNull();

        // Customer balance should be restored back to 0
        var custAfterDelete = await context.Customers.FindAsync(customer.Id);
        custAfterDelete!.CurrentBalance.Should().Be(0m);
    }

    [Fact]
    public async Task DeleteTransactionAsync_WhenAlreadyCancelled_DeletesWithoutDoubleRevertingBalance()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Jamuna Printing House",
            OpeningBalance = 2000m,
            CurrentBalance = 2000m
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var service = new LedgerService(context, NullLogger<LedgerService>.Instance);

        var dto = new CreateTransactionDto
        {
            TransactionDate = new DateOnly(2026, 10, 21),
            CustomerId = customer.Id,
            WorkDescription = "Mistake order",
            Discount = 0m,
            Items = new List<CreateTransactionItemDto>
            {
                new() { MaterialName = "Offset 70 GSM", Quantity = 2m, UnitRate = 1000m } // 2000 due
            }
        };

        var created = await service.CreateTransactionAsync(dto);
        var custAfterCreate = await context.Customers.FindAsync(customer.Id);
        custAfterCreate!.CurrentBalance.Should().Be(4000m);

        // Cancel first
        await service.CancelTransactionAsync(created.Id, "Wrong entry");
        var custAfterCancel = await context.Customers.FindAsync(customer.Id);
        custAfterCancel!.CurrentBalance.Should().Be(2000m);

        // Act - Delete the cancelled transaction
        bool result = await service.DeleteTransactionAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var deletedTxn = await context.Transactions.FindAsync(created.Id);
        deletedTxn.Should().BeNull();

        // Balance should remain 2000m without double deduction
        var custAfterDelete = await context.Customers.FindAsync(customer.Id);
        custAfterDelete!.CurrentBalance.Should().Be(2000m);
    }

    [Fact]
    public async Task DeleteTransactionAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new LedgerService(context, NullLogger<LedgerService>.Instance);

        // Act
        bool result = await service.DeleteTransactionAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTransactionAsync_WithCustomAgreedWorkPrice_CalculatesInvestmentAndProfitAccurately()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Padma Packaging Ltd",
            PhoneNumber = "01999887766",
            OpeningBalance = 0m,
            CurrentBalance = 0m
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var service = new LedgerService(context, NullLogger<LedgerService>.Instance);

        // Investment: Paper (5 * 1000 = 5000) + Plate (1500) + Print (2500) = 9000
        // Customer agreed price = 15000, Discount = 500 => Total Bill = 14500
        // Profit = 14500 - 9000 = 5500
        var dto = new CreateTransactionDto
        {
            TransactionDate = new DateOnly(2026, 10, 23),
            CustomerId = customer.Id,
            WorkDescription = "10,000 Medicine Carton Boxes",
            SubTotal = 15000m,
            Discount = 500m,
            Items = new List<CreateTransactionItemDto>
            {
                new()
                {
                    MaterialName = "Duplex Board 300 GSM",
                    Quantity = 5m,
                    UnitRate = 1000m
                }
            },
            CostEntries = new List<CreateCostEntryDto>
            {
                new()
                {
                    Category = CostCategory.Plate,
                    Description = "CTP Plates",
                    Amount = 1500m
                },
                new()
                {
                    Category = CostCategory.Printing,
                    Description = "Offset 4-Color Printing",
                    Amount = 2500m
                }
            },
            InitialPaymentAmount = 4500m,
            InitialPaymentMethod = PaymentMethod.Cash
        };

        // Act
        var created = await service.CreateTransactionAsync(dto);

        // Assert
        created.Should().NotBeNull();
        created.TotalDirectCost.Should().Be(9000m); // Total Investment
        created.SubTotal.Should().Be(15000m);       // Agreed Job Value
        created.TotalBillAmount.Should().Be(14500m); // Net Bill to Customer
        created.Profit.Should().Be(5500m);          // 14500 - 9000 = 5500
        created.ProfitMargin.Should().Be(37.9m);    // (5500 / 14500) * 100 = 37.9%
        created.BalanceDue.Should().Be(10000m);     // 14500 - 4500 = 10000
    }
}
