using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmAccountEndpoints
{
    private const int MaxRetries = 3;

    public static void MapCrmAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/accounts")
            .WithTags("CRM Accounts")
            .AddEndpointFilter(StaffAuthFilter);

        group.MapGet("/{id:int}", async (int id, BankDbContext db) =>
        {
            var account = await db.Accounts
                .Include(a => a.Customer)
                .Include(a => a.OffsetAccounts)
                .FirstOrDefaultAsync(a => a.Id == id);

            return account is null ? Results.NotFound() : Results.Ok(account);
        });

        group.MapPost("/{id:int}/adjust", async (int id, AdjustBalanceRequest req, BankDbContext db, HttpContext http) =>
        {
            var staffId = StaffAuthService.GetStaffIdFromContext(http);
            var staff = await db.StaffUsers.FindAsync(staffId);
            if (staff is null) return Results.Unauthorized();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                var account = await db.Accounts.FindAsync(id);
                if (account is null) return Results.NotFound();
                if (!account.IsActive) return Results.BadRequest(new { error = "Cannot adjust a closed account" });

                account.Balance += req.Amount;

                var txn = new Transaction
                {
                    AccountId = id,
                    Amount = req.Amount,
                    Description = $"Manual adjustment by {staff.DisplayName}: {req.Reason}",
                    TransactionType = TransactionType.Adjustment,
                    BalanceAfter = account.Balance
                };

                db.Transactions.Add(txn);

                try
                {
                    await db.SaveChangesAsync();
                    return Results.Ok(new { transaction = txn, newBalance = account.Balance });
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
                {
                    db.ChangeTracker.Clear();
                }
            }

            return Results.Conflict(new { error = "Unable to complete adjustment due to concurrent access. Please try again." });
        });

        group.MapPost("/{id:int}/close", async (int id, CloseAccountRequest? req, BankDbContext db, HttpContext http) =>
        {
            var account = await db.Accounts.FindAsync(id);
            if (account is null) return Results.NotFound();

            if (!account.IsActive)
                return Results.BadRequest(new { error = "Account is already closed" });

            if (account.Balance != 0 && !(req?.Force ?? false))
                return Results.BadRequest(new { error = "Account has a non-zero balance. Use force=true to close anyway." });

            account.IsActive = false;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Account closed", account.Id, account.IsActive });
        });
    }

    private static async ValueTask<object?> StaffAuthFilter(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var staffId = StaffAuthService.GetStaffIdFromContext(context.HttpContext);
        if (staffId is null)
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
        return await next(context);
    }
}

public record AdjustBalanceRequest(decimal Amount, string Reason);
public record CloseAccountRequest(bool Force);
