using Microsoft.AspNetCore.Mvc.Filters;
using Sample.License.Web.Data.Context;
using Sample.License.Web.Middlewares;
using Sample.License.Web.Services;

namespace Sample.License.Web.Attributes;

/// <summary>
///     Attribute for authorizing access to an action.
/// </summary>
/// <param name="allowAnonymous">Indicates whether anonymous access is allowed.</param>
/// <param name="roles">The roles that are authorized to access the action.</param>
public class AuthorizeMiddlewareAttribute(bool allowAnonymous, params string[] roles) : Attribute, IAsyncActionFilter
{
    public readonly bool AllowAnonymous = allowAnonymous;
    public readonly string[] Roles = roles;

    public AuthorizeMiddlewareAttribute(params string[] roles) : this(false, roles)
    {
    }

    public AuthorizeMiddlewareAttribute() : this(true, ["User"])
    {
    }


    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var authService = httpContext.RequestServices.GetRequiredService<IAuthService>();
        var dbContext = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var middleware =
            new AuthorizationMiddleware(async _ => await next(), authService, dbContext, Roles, AllowAnonymous);
        await middleware.InvokeAsync(httpContext);
    }
}