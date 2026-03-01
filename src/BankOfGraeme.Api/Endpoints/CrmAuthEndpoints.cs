using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmAuthEndpoints
{
    public static void MapCrmAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm").WithTags("CRM Auth");

        group.MapPost("/login", async (LoginRequest req, StaffAuthService auth) =>
        {
            var result = await auth.LoginAsync(req.Username, req.Password);
            return result is null
                ? Results.Unauthorized()
                : Results.Ok(result);
        });
    }
}

public record LoginRequest(string Username, string Password);
