using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using AccountManagement.Infrastructure.Persistence;
using AccountManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class MemoServiceTests
{
    private AccountDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task GenerateTransactionMemoHtmlAsync_ReturnsRichPrintableHtml_WithAllDetails()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Al-Madina Printing & Packaging",
            PhoneNumber = "01711223344",
            Address = "12 Arambagh, Dhaka",
            OpeningBalance = 1500m,
            CurrentBalance = 9000m
        };
        await context.Customers.AddAsync(customer);

        var transaction = new Transaction
        {
            TransactionNumber = "JOB-20261021-0001",
            TransactionDate = new DateOnly(2026, 10, 21),
            CustomerId = customer.Id,
            Customer = customer,
            WorkDescription = "5000 Cash Memo Booklets with 4-Color Cover",
            Discount = 500m,
            SubTotal = 12000m,
            TotalBillAmount = 11500m,
            TotalPaidAmount = 4000m,
            BalanceDue = 7500m,
            Status = TransactionStatus.Active,
            Notes = "Urgent delivery required by Friday."
        };

        transaction.Items.Add(new TransactionItem
        {
            MaterialNameSnapshot = "Art Card 300 GSM",
            GsmSnapshot = "300 GSM",
            SizeSnapshot = "22x28",
            Quantity = 4m,
            Unit = UnitOfMeasure.Packet,
            UnitRateSnapshot = 1500m,
            TotalMaterialCost = 6000m
        });

        transaction.CostEntries.Add(new CostEntry
        {
            Category = CostCategory.Plate,
            Description = "4-Color CTP Plate Making",
            Amount = 1000m
        });

        transaction.CostEntries.Add(new CostEntry
        {
            Category = CostCategory.Printing,
            Description = "Multi-color Offset Printing",
            Amount = 5000m
        });

        transaction.Payments.Add(new Payment
        {
            PaymentNumber = "RCT-20261021-0001",
            PaymentDate = new DateOnly(2026, 10, 21),
            Amount = 4000m,
            PaymentMethod = PaymentMethod.BankTransfer,
            ReferenceNumber = "TRX-987654",
            CustomerId = customer.Id
        });

        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();

        var memoService = new MemoService(context, NullLogger<MemoService>.Instance);

        // Act
        var html = await memoService.GenerateTransactionMemoHtmlAsync(transaction.Id);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("রূপালী প্রেস অ্যান্ড প্যাকেজিং");
        html.Should().Contain("RUPALI PRESS &amp; PACKAGING");
        html.Should().Contain("JOB-20261021-0001");
        html.Should().Contain("Al-Madina Printing & Packaging");
        html.Should().Contain("01711223344");
        html.Should().Contain("5000 Cash Memo Booklets with 4-Color Cover");
        html.Should().Contain("Art Card 300 GSM");
        html.Should().Contain("4-Color CTP Plate Making");
        html.Should().Contain("Multi-color Offset Printing");
        html.Should().Contain("11,500.00");
        html.Should().Contain("4,000.00");
        html.Should().Contain("7,500.00");
        html.Should().Contain("In Words");
        html.Should().Contain("টাকা মাত্র");
        html.Should().Contain("window.print()");
    }

    [Fact]
    public async Task GenerateTransactionMemoHtmlAsync_ThrowsKeyNotFoundException_WhenNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var memoService = new MemoService(context, NullLogger<MemoService>.Instance);

        // Act
        var act = () => memoService.GenerateTransactionMemoHtmlAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GenerateCustomerStatementMemoHtmlAsync_ReturnsDetailedStatementHtml()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Green Leaf Publications",
            PhoneNumber = "01812345678",
            Address = "Banglabazar, Dhaka",
            OpeningBalance = 2000m,
            CurrentBalance = 7500m
        };
        await context.Customers.AddAsync(customer);

        var txn = new Transaction
        {
            TransactionNumber = "JOB-20261022-0001",
            TransactionDate = new DateOnly(2026, 10, 22),
            CustomerId = customer.Id,
            Customer = customer,
            WorkDescription = "1000 Catalogues Print &amp; Binding",
            SubTotal = 10000m,
            TotalBillAmount = 10000m,
            TotalPaidAmount = 4500m,
            BalanceDue = 5500m,
            Status = TransactionStatus.Active
        };
        await context.Transactions.AddAsync(txn);

        var pmt = new Payment
        {
            PaymentNumber = "RCT-20261022-0001",
            PaymentDate = new DateOnly(2026, 10, 22),
            CustomerId = customer.Id,
            Customer = customer,
            Amount = 4500m,
            PaymentMethod = PaymentMethod.bKash,
            ReferenceNumber = "BK-778899"
        };
        await context.Payments.AddAsync(pmt);

        await context.SaveChangesAsync();

        var memoService = new MemoService(context, NullLogger<MemoService>.Instance);

        // Act
        var html = await memoService.GenerateCustomerStatementMemoHtmlAsync(customer.Id);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("গ্রাহকের খতিয়ান হিসাব বিবরণী");
        html.Should().Contain("Green Leaf Publications");
        html.Should().Contain("Banglabazar, Dhaka");
        html.Should().Contain("JOB-20261022-0001");
        html.Should().Contain("RCT-20261022-0001");
        html.Should().Contain("7,500.00");
    }

    [Fact]
    public async Task SaveMemoHtmlToFileAsync_WritesFileToDiskAndReturnsPath()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var memoService = new MemoService(context, NullLogger<MemoService>.Instance);
        var sampleHtml = "<!DOCTYPE html><html><body><h1>Test Memo</h1></body></html>";

        // Act
        var filePath = await memoService.SaveMemoHtmlToFileAsync(sampleHtml, "unit_test_memo.html");

        // Assert
        filePath.Should().NotBeNullOrWhiteSpace();
        File.Exists(filePath).Should().BeTrue();
        var readContent = await File.ReadAllTextAsync(filePath);
        readContent.Should().Be(sampleHtml);

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
