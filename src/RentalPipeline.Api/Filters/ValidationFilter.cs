using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace RentalPipeline.Api.Filters;

/// <summary>
/// Automatically validates every action argument that has a registered <see cref="IValidator{T}"/>
/// (Architecture.md: "Register FluentValidation pipeline (auto-validate + 400 on invalid
/// requests)"). Short-circuits with an RFC 7807 <c>ValidationProblemDetails</c> <c>400</c> response
/// on failure, so controllers and DTOs never need explicit validation calls or Data Annotations.
/// </summary>
/// <remarks>
/// The official <c>FluentValidation.AspNetCore</c> auto-validation package has been deprecated by
/// its author since 2021, so this project wires the equivalent behavior itself via a plain
/// <see cref="IAsyncActionFilter"/> instead of taking on an unmaintained dependency.
/// </remarks>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    /// <summary>Creates the filter with the services it needs to resolve validators and build ProblemDetails.</summary>
    public ValidationFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory)
    {
        _serviceProvider = serviceProvider;
        _problemDetailsFactory = problemDetailsFactory;
    }

    /// <summary>Validates action arguments before execution, short-circuiting with 400 on failure.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext);

            foreach (var error in validationResult.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var problemDetails = _problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState);
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }

        await next();
    }
}
