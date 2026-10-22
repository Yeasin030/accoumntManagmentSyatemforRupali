using AccountManagement.Application.DTOs;
using FluentValidation;

namespace AccountManagement.Application.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(150).WithMessage("Name cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(?:\+?88)?01[3-9]\d{8}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Please enter a valid Bangladeshi mobile number (e.g., 017xxxxxxxx).");

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Opening balance cannot be negative.");
    }
}

public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer selection is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
    }
}

public class CreateExpenseValidator : AbstractValidator<CreateExpenseDto>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Expense category is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Expense description is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Expense amount must be greater than zero.");
    }
}

public class CreateOrUpdateMaterialRateValidator : AbstractValidator<CreateOrUpdateMaterialRateDto>
{
    public CreateOrUpdateMaterialRateValidator()
    {
        RuleFor(x => x.RatePeriodId)
            .NotEmpty().WithMessage("Rate period must be selected.");

        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("Material must be selected.");

        RuleFor(x => x.RatePerUnit)
            .GreaterThanOrEqualTo(0).WithMessage("Rate per unit cannot be negative.");

        RuleFor(x => x.MinQuantityTier)
            .GreaterThan(0).WithMessage("Minimum quantity tier must be greater than zero.");
    }
}
