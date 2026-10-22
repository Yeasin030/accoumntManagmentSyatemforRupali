using AccountManagement.Application.DTOs;
using FluentValidation;

namespace AccountManagement.Application.Validators;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer selection is required.");

        RuleFor(x => x.WorkDescription)
            .NotEmpty().WithMessage("Work description is required.")
            .MaximumLength(500).WithMessage("Work description cannot exceed 500 characters.");

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

        RuleForEach(x => x.Items).SetValidator(new CreateTransactionItemValidator());
        RuleForEach(x => x.CostEntries).SetValidator(new CreateCostEntryValidator());
    }
}

public class CreateTransactionItemValidator : AbstractValidator<CreateTransactionItemDto>
{
    public CreateTransactionItemValidator()
    {
        RuleFor(x => x.MaterialName)
            .NotEmpty().WithMessage("Material name is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitRate)
            .GreaterThanOrEqualTo(0).WithMessage("Unit rate cannot be negative.");
    }
}

public class CreateCostEntryValidator : AbstractValidator<CreateCostEntryDto>
{
    public CreateCostEntryValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Cost description is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Cost amount must be greater than zero.");
    }
}
