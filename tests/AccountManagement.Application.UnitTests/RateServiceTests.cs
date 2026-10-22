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

public class RateServiceTests
{
    private AccountDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task ResolveRateAsync_WithExplicitStandardMatch_ReturnsExactRate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var material = new Material
        {
            Name = "Partex Offset Paper",
            Code = "POP-70",
            DefaultGsm = "70 GSM",
            DefaultSize = "18/23",
            DefaultUnit = UnitOfMeasure.Ream,
            IsActive = true
        };
        await context.Materials.AddAsync(material);

        var period = new RatePeriod
        {
            Name = "October 2026",
            Year = 2026,
            Month = 10,
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 10, 31),
            IsActive = true
        };
        await context.RatePeriods.AddAsync(period);

        var rate = new MaterialRate
        {
            RatePeriod = period,
            Material = material,
            Gsm = "70 GSM",
            Size = "18/23",
            MinQuantityTier = 1,
            RatePerUnit = 2600m,
            Unit = UnitOfMeasure.Ream
        };
        await context.MaterialRates.AddAsync(rate);
        await context.SaveChangesAsync();

        var service = new RateService(context, NullLogger<RateService>.Instance);

        // Act
        var resolved = await service.ResolveRateAsync(
            material.Id,
            new DateOnly(2026, 10, 15),
            gsm: "70 GSM",
            size: "18/23",
            quantity: 5);

        // Assert
        resolved.Should().Be(2600m);
    }

    [Fact]
    public async Task ResolveRateAsync_WithExplicitCutSize_ReturnsExplicitCutRate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var material = new Material
        {
            Name = "Swedish Art Paper",
            Code = "SAP-80",
            DefaultGsm = "80 GSM",
            DefaultSize = "18/23",
            DefaultUnit = UnitOfMeasure.Piece,
            IsActive = true
        };
        await context.Materials.AddAsync(material);

        var period = new RatePeriod
        {
            Name = "October 2026",
            Year = 2026,
            Month = 10,
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 10, 31),
            IsActive = true
        };
        await context.RatePeriods.AddAsync(period);

        // Explicit rate seeded for cut size 9/11.5 per 1000 pieces
        var cutRate = new MaterialRate
        {
            RatePeriod = period,
            Material = material,
            Gsm = "80 GSM",
            Size = "9/11.5",
            MinQuantityTier = 1000,
            RatePerUnit = 1450m,
            Unit = UnitOfMeasure.Piece
        };
        await context.MaterialRates.AddAsync(cutRate);
        await context.SaveChangesAsync();

        var service = new RateService(context, NullLogger<RateService>.Instance);

        // Act
        var resolved = await service.ResolveRateAsync(
            material.Id,
            new DateOnly(2026, 10, 20),
            gsm: "80 GSM",
            size: "9/11.5",
            quantity: 1000);

        // Assert
        resolved.Should().Be(1450m);
    }

    [Fact]
    public async Task ResolveRateAsync_WhenCutSizeHasNoExplicitRate_CalculatesFromBase18x23Rate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var material = new Material
        {
            Name = "White Print Paper",
            Code = "WPP-70",
            DefaultGsm = "70 GSM",
            DefaultSize = "18/23",
            DefaultUnit = UnitOfMeasure.Ream,
            IsActive = true
        };
        await context.Materials.AddAsync(material);

        var period = new RatePeriod
        {
            Name = "October 2026",
            Year = 2026,
            Month = 10,
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 10, 31),
            IsActive = true
        };
        await context.RatePeriods.AddAsync(period);

        // Only standard 18/23 is seeded at 2000 BDT per Ream
        var baseReamRate = new MaterialRate
        {
            RatePeriod = period,
            Material = material,
            Gsm = "70 GSM",
            Size = "18/23",
            MinQuantityTier = 1,
            RatePerUnit = 2000m,
            Unit = UnitOfMeasure.Ream
        };
        await context.MaterialRates.AddAsync(baseReamRate);
        await context.SaveChangesAsync();

        var service = new RateService(context, NullLogger<RateService>.Instance);

        // Act: User requests 9/11.5 (Quarter Demy - 4 cuts per 18/23 sheet)
        // 1000 full sheets = 2 Reams = 4000 BDT.
        // 1000 pieces of 9/11.5 = 4000 / 4 = 1000 BDT.
        var resolved = await service.ResolveRateAsync(
            material.Id,
            new DateOnly(2026, 10, 10),
            gsm: "70 GSM",
            size: "9/11.5",
            quantity: 1);

        // Assert
        resolved.Should().Be(1000m);
    }

    [Fact]
    public async Task SaveRateAsync_CreatesNewRate_AndUpdatesExistingRate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var category = new MaterialCategory { Name = "Offset", DisplayOrder = 1 };
        await context.MaterialCategories.AddAsync(category);

        var material = new Material { Name = "Amber Offset", Code = "AMB", Category = category, IsActive = true };
        await context.Materials.AddAsync(material);

        var period = new RatePeriod
        {
            Name = "October 2026",
            Year = 2026,
            Month = 10,
            EffectiveStartDate = new DateOnly(2026, 10, 1),
            EffectiveEndDate = new DateOnly(2026, 10, 31),
            IsActive = true
        };
        await context.RatePeriods.AddAsync(period);
        await context.SaveChangesAsync();

        var service = new RateService(context, NullLogger<RateService>.Instance);

        // Act 1: Create Rate
        var createDto = new CreateOrUpdateMaterialRateDto
        {
            RatePeriodId = period.Id,
            MaterialId = material.Id,
            Gsm = "80 GSM",
            Size = "18/23",
            MinQuantityTier = 1,
            RatePerUnit = 3200m,
            Unit = UnitOfMeasure.Ream,
            Remarks = "Standard ream rate"
        };
        var created = await service.SaveRateAsync(createDto);

        // Assert 1
        created.RatePerUnit.Should().Be(3200m);
        context.MaterialRates.Count().Should().Be(1);

        // Act 2: Update existing Rate with new price
        var updateDto = new CreateOrUpdateMaterialRateDto
        {
            RatePeriodId = period.Id,
            MaterialId = material.Id,
            Gsm = "80 GSM",
            Size = "18/23",
            MinQuantityTier = 1,
            RatePerUnit = 3450m,
            Unit = UnitOfMeasure.Ream,
            Remarks = "Updated market price"
        };
        var updated = await service.SaveRateAsync(updateDto);

        // Assert 2
        updated.RatePerUnit.Should().Be(3450m);
        updated.Remarks.Should().Be("Updated market price");
        context.MaterialRates.Count().Should().Be(1);
    }
}
