using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(AccountDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers
            .Include(c => c.Transactions)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                     (c.PhoneNumber != null && c.PhoneNumber.Contains(term)) ||
                                     (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                ContactPerson = c.ContactPerson,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                OpeningBalance = c.OpeningBalance,
                CurrentBalance = c.CurrentBalance,
                IsActive = c.IsActive,
                Notes = c.Notes,
                TotalTransactionsCount = c.Transactions.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null) return null;

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            OpeningBalance = customer.OpeningBalance,
            CurrentBalance = customer.CurrentBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            TotalTransactionsCount = customer.Transactions.Count
        };
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            OpeningBalance = dto.OpeningBalance,
            CurrentBalance = dto.OpeningBalance,
            Notes = dto.Notes,
            IsActive = true
        };

        await _context.Customers.AddAsync(customer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new customer: {CustomerName}", customer.Name);

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            OpeningBalance = customer.OpeningBalance,
            CurrentBalance = customer.CurrentBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            TotalTransactionsCount = 0
        };
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer {id} not found.");
        }

        customer.Name = dto.Name.Trim();
        customer.ContactPerson = dto.ContactPerson?.Trim();
        customer.PhoneNumber = dto.PhoneNumber?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Notes = dto.Notes;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            ContactPerson = customer.ContactPerson,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            OpeningBalance = customer.OpeningBalance,
            CurrentBalance = customer.CurrentBalance,
            IsActive = customer.IsActive,
            Notes = customer.Notes
        };
    }
}
