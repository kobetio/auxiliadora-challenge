using FluentValidation;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(30);
    }
}
