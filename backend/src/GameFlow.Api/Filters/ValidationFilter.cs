using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameFlow.Api.Filters;

/// <summary>
/// Eylem parametrelerini, kayıtlı FluentValidation doğrulayıcılarıyla otomatik doğrular.
/// Doğrulama hatası bulunursa istisna fırlatılır ve
/// <see cref="Middleware.ExceptionHandlingMiddleware"/> bunu 400 yanıtına çevirir.
/// Böylece her controller'da tekrar eden doğrulama kodu ortadan kalkar.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        await next();
    }
}
