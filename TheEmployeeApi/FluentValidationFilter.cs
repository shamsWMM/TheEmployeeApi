using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

public class FluentValidationFilter(IServiceProvider serviceProvider, ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue) && argumentValue != null)
            {
                var argumentType = argumentValue.GetType();

                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

                var validator = serviceProvider.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    // Validate the argument
                    ValidationResult validationResult = await validator.ValidateAsync(new ValidationContext<object>(argumentValue));

                    if (!validationResult.IsValid)
                    {
                        validationResult.AddToModelState(context.ModelState);
                        var problemDetails =
                            problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext,
                                context.ModelState);
                        context.Result = new BadRequestObjectResult(problemDetails);

                        return;
                    }
                }
            }
        }

        await next();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}