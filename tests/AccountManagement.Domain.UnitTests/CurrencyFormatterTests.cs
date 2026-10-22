using AccountManagement.Domain.Formatting;
using FluentAssertions;
using Xunit;

namespace AccountManagement.Domain.UnitTests;

public class CurrencyFormatterTests
{
    [Fact]
    public void FormatBdt_StandardAmount_FormatsWithBangladeshiLakhGrouping()
    {
        // Arrange
        decimal amount = 125000.50m;

        // Act
        string formatted = CurrencyFormatter.FormatBdt(amount, includeSymbol: true);

        // Assert
        formatted.Should().Be("৳ 1,25,000.50");
    }

    [Fact]
    public void FormatBdt_CroreAmount_FormatsCorrectly()
    {
        // Arrange
        decimal amount = 12345678.90m;

        // Act
        string formatted = CurrencyFormatter.FormatBdt(amount, includeSymbol: true);

        // Assert
        formatted.Should().Be("৳ 1,23,45,678.90");
    }

    [Fact]
    public void FormatBanglaBdt_FormatsWithBanglaNumerals()
    {
        // Arrange
        decimal amount = 50250.00m;

        // Act
        string formatted = CurrencyFormatter.FormatBanglaBdt(amount, includeSymbol: true);

        // Assert
        formatted.Should().Be("৳ ৫০,২৫০.০০");
    }

    [Fact]
    public void ConvertToEnglishDigits_ConvertsBengaliNumeralsToAscii()
    {
        // Arrange
        string banglaInput = "৳ ১,২৫,০০০.৫০";

        // Act
        string englishResult = CurrencyFormatter.ConvertToEnglishDigits(banglaInput);

        // Assert
        englishResult.Should().Be("125000.50");
    }
}
