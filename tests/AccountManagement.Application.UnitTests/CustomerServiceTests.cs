using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using AccountManagement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AccountManagement.Application.UnitTests;

public class CustomerServiceTests
{
    private AccountDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountDbContext(options);
    }

    [Fact]
    public async Task CreateCustomerAsync_CreatesCustomerWithOpeningBalanceAndActiveStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CustomerService(context, NullLogger<CustomerService>.Instance);

        var dto = new CreateCustomerDto
        {
            Name = "Rupali Offset Press",
            ContactPerson = "Md. Rafiqul Islam",
            PhoneNumber = "01819123456",
            Email = "rupali.press@gmail.com",
            Address = "12 Arambagh, Motijheel, Dhaka",
            OpeningBalance = 15000m,
            Notes = "VIP Press Client"
        };

        // Act
        var created = await service.CreateCustomerAsync(dto);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("Rupali Offset Press");
        created.PhoneNumber.Should().Be("01819123456");
        created.OpeningBalance.Should().Be(15000m);
        created.CurrentBalance.Should().Be(15000m);
        created.IsActive.Should().BeTrue();

        var inDb = await context.Customers.FindAsync(created.Id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be("Rupali Offset Press");
    }

    [Fact]
    public async Task UpdateCustomerAsync_UpdatesCustomerInformation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var customer = new Customer
        {
            Name = "Original Name Press",
            PhoneNumber = "01700000000",
            Address = "Paltan, Dhaka",
            OpeningBalance = 5000m,
            CurrentBalance = 5000m,
            IsActive = true
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var service = new CustomerService(context, NullLogger<CustomerService>.Instance);

        var updateDto = new CreateCustomerDto
        {
            Name = "Updated Name Press",
            ContactPerson = "Hasan Mahmud",
            PhoneNumber = "01711998877",
            Address = "Fakirapool, Motijheel, Dhaka",
            Notes = "Updated contact details"
        };

        // Act
        var updated = await service.UpdateCustomerAsync(customer.Id, updateDto);

        // Assert
        updated.Should().NotBeNull();
        updated.Name.Should().Be("Updated Name Press");
        updated.ContactPerson.Should().Be("Hasan Mahmud");
        updated.PhoneNumber.Should().Be("01711998877");
        updated.Address.Should().Be("Fakirapool, Motijheel, Dhaka");

        var inDb = await context.Customers.FindAsync(customer.Id);
        inDb!.Name.Should().Be("Updated Name Press");
        inDb.PhoneNumber.Should().Be("01711998877");
    }

    [Fact]
    public async Task GetAllCustomersAsync_WithSearchTerm_FiltersCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Customers.AddRangeAsync(
            new Customer { Name = "Dhaka Printing Corp", PhoneNumber = "01711111111", IsActive = true },
            new Customer { Name = "Chittagong Color Lab", PhoneNumber = "01822222222", ContactPerson = "Faruk Ahmed", IsActive = true },
            new Customer { Name = "Arambagh Press", PhoneNumber = "01933333333", IsActive = true }
        );
        await context.SaveChangesAsync();

        var service = new CustomerService(context, NullLogger<CustomerService>.Instance);

        // Act 1: Search by name partial
        var result1 = await service.GetAllCustomersAsync("Color");
        result1.Should().HaveCount(1);
        result1[0].Name.Should().Be("Chittagong Color Lab");

        // Act 2: Search by phone number
        var result2 = await service.GetAllCustomersAsync("019333");
        result2.Should().HaveCount(1);
        result2[0].Name.Should().Be("Arambagh Press");

        // Act 3: Search by contact person
        var result3 = await service.GetAllCustomersAsync("Faruk");
        result3.Should().HaveCount(1);
        result3[0].Name.Should().Be("Chittagong Color Lab");

        // Act 4: Empty search returns all
        var resultAll = await service.GetAllCustomersAsync();
        resultAll.Should().HaveCount(3);
    }
}
