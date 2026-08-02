using FluentValidation;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Validators;

public class CreateProposalValidator : AbstractValidator<CreateProposalRequest>
{
    public CreateProposalValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .NotEmpty();
    }
}
