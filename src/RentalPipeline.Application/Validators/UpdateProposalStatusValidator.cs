using FluentValidation;
using RentalPipeline.Application.DTOs;

namespace RentalPipeline.Application.Validators;

public class UpdateProposalStatusValidator : AbstractValidator<UpdateProposalStatusRequest>
{
    public UpdateProposalStatusValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum();
    }
}
