using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmScheduledPaymentEndpoints
{
    public static void MapCrmScheduledPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/accounts/{accountId:int}/scheduled-payments")
            .WithTags("CRM Scheduled Payments")
            .AddEndpointFilter(StaffAuthFilter);

        group.MapGet("/", async (int accountId, BankDbContext db) =>
        {
            var payments = await db.ScheduledPayments
                .Where(sp => sp.AccountId == accountId)
                .OrderByDescending(sp => sp.IsActive)
                .ThenBy(sp => sp.NextDueDate)
                .Select(sp => new
                {
                    sp.Id,
                    sp.AccountId,
                    sp.PayeeName,
                    sp.PayeeBsb,
                    sp.PayeeAccountNumber,
                    sp.PayeeAccountId,
                    sp.Amount,
                    sp.Description,
                    sp.Reference,
                    Frequency = sp.Frequency.ToString(),
                    StartDate = sp.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = sp.EndDate.HasValue ? sp.EndDate.Value.ToString("yyyy-MM-dd") : null,
                    NextDueDate = sp.NextDueDate.ToString("yyyy-MM-dd"),
                    sp.IsActive,
                    sp.CreatedAt,
                })
                .ToListAsync();

            return Results.Ok(payments);
        });
    }

    private static async ValueTask<object?> StaffAuthFilter(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetRequiredService<StaffAuthService>();
        var staffId = auth.GetStaffIdFromContext(context.HttpContext);
        if (staffId is null)
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
        return await next(context);
    }
}
