using EdSkill.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EdSkill.API.Middleware;

public class SuspendedUserMiddleware
{
    private readonly RequestDelegate _next;

    public SuspendedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null && user.Status == "suspended")
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        ErrorCode = "ACCOUNT_SUSPENDED",
                        ErrorMessage = "Account is suspended"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
