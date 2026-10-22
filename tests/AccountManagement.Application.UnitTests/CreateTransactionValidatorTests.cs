using AccountManagement.Application.DTOs;
using AccountManagement.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    [Fact]
    public void Validate_ValidTransaction_ReturnsValid()
    {
        // Arrange
        var dto = new CreateTransactionDto
        {
            CustomerId = Guid.NewGuid(),
            WorkDescription = "1000 Pcs Cash Memo 1/8 Demy 3-Part",
            Discount = 100m,
            Items = new List<CreateTransactionItemDto>
            {
                new()
                {
                    MaterialName = "Auto Carbon Top (CB)",
                    Quantity = 2m,
                    UnitRate = 750m
                }
            },
            CostEntries = new List<CreateCostEntryDto>
            {
                new()
                {
                    Category = Domain.Enums.CostCategory.Printing,
                    Description = "Single color offset printing",
                    Amount = 500m
                }
            }
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingCustomer_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateTransactionDto
        {
            CustomerId = Guid.Empty,
            WorkDescription = "Valid Description"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Validate_NegativeDiscount_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateTransactionDto
        {
            CustomerId = Guid.NewGuid(),
            WorkDescription = "Test Job",
            Discount = -50m
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Discount");
    }
}
