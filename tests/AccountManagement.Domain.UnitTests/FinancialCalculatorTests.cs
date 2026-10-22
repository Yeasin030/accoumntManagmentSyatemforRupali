using AccountManagement.Domain.Services;
using FluentAssertions;
using Xunit;

namespace AccountManagement.Domain.UnitTests;

public class FinancialCalculatorTests
{
    [Theory]
    [InlineData(10.5, 250.0, 2625.00)]
    [InlineData(1.333, 150.0, 199.95)]
    [InlineData(0, 500.0, 0.00)]
    public void CalculateMaterialCost_WithValidInputs_ReturnsExpectedResult(decimal qty, decimal rate, decimal expected)
    {
        // Act
        decimal result = FinancialCalculator.CalculateMaterialCost(qty, rate);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateMaterialCost_NegativeValues_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => FinancialCalculator.CalculateMaterialCost(-5m, 100m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateTotalBill_WithDiscount_AppliesDiscountAccurately()
    {
        // Arrange
        decimal subTotal = 15000.00m;
        decimal discount = 1500.00m;

        // Act
        decimal totalBill = FinancialCalculator.CalculateTotalBill(subTotal, discount);

        // Assert
        totalBill.Should().Be(13500.00m);
    }

    [Fact]
    public void CalculateTotalBill_DiscountExceedingSubtotal_ReturnsZero()
    {
        // Arrange
        decimal subTotal = 5000.00m;
        decimal discount = 7000.00m;

        // Act
        decimal totalBill = FinancialCalculator.CalculateTotalBill(subTotal, discount);

        // Assert
        totalBill.Should().Be(0m);
    }

    [Fact]
    public void CalculateOutstandingBalance_WithPartialPayment_ReturnsRemainingDue()
    {
        // Arrange
        decimal bill = 25000.00m;
        decimal paid = 10000.00m;

        // Act
        decimal balance = FinancialCalculator.CalculateOutstandingBalance(bill, paid);

        // Assert
        balance.Should().Be(15000.00m);
    }

    [Fact]
    public void CalculateGrossProfit_CalculatesRevenueMinusDirectCost()
    {
        // Arrange
        decimal revenue = 30000.00m;
        decimal directCost = 21500.00m;

        // Act
        decimal profit = FinancialCalculator.CalculateGrossProfit(revenue, directCost);

        // Assert
        profit.Should().Be(8500.00m);
    }

    [Theory]
    [InlineData(500, 1.0)]
    [InlineData(250, 0.5)]
    [InlineData(1250, 2.5)]
    public void ConvertSheetsToReams_CalculatesCorrectReams(decimal sheets, decimal expectedReams)
    {
        // Act
        decimal reams = FinancialCalculator.ConvertSheetsToReams(sheets, 500);

        // Assert
        reams.Should().Be(expectedReams);
    }

    [Theory]
    [InlineData(6000, "9/11.5", 1500.00)] // Quarter Demy / 4-cut
    [InlineData(6000, "18/23", 6000.00)]  // Full sheet
    [InlineData(6000, "9/14", 2033.90)]   // 3-cut with trim (divisor 2.95)
    [InlineData(6000, "7.5/9", 1016.95)]  // 6-cut with trim (divisor 5.9)
    [InlineData(3420, "9/11.5", 855.00)]  // 80 GSM Art Paper quarter demy
    public void CalculateCutSizePrice_ReturnsExpectedCutRates(decimal baseRate, string size, decimal expected)
    {
        // Act
        decimal result = FinancialCalculator.CalculateCutSizePrice(baseRate, size);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Single Plate", 2, 140.00)]
    [InlineData("Full Plate", 1, 140.00)]
    [InlineData("Demy Plate", 3, 600.00)]
    [InlineData("সিঙ্গেল প্লেট", 1, 70.00)]
    public void CalculatePlateCost_CalculatesCorrectPlateCharge(string plateType, int quantity, decimal expected)
    {
        // Act
        decimal cost = FinancialCalculator.CalculatePlateCost(plateType, quantity);

        // Assert
        cost.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1000, 150.00)]
    [InlineData(2, 1000, 300.00)]
    [InlineData(3, 1000, 450.00)]
    [InlineData(4, 1000, 600.00)]
    [InlineData(2, 2500, 900.00)] // 3 x 2 x 150
    public void CalculatePrintingCost_CalculatesPerColorPerThousand(int colors, decimal sheets, decimal expected)
    {
        // Act
        decimal cost = FinancialCalculator.CalculatePrintingCost(colors, sheets);

        // Assert
        cost.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 12.00)]
    [InlineData(10, 120.00)]
    [InlineData(50, 600.00)]
    public void CalculateCashMemoBindingCost_CalculatesCorrectBindingCost(int books, decimal expected)
    {
        // Act
        decimal cost = FinancialCalculator.CalculateCashMemoBindingCost(books);

        // Assert
        cost.Should().Be(expected);
    }
}

