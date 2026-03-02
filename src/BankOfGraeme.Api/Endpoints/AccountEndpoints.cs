using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using BankOfGraeme.Api.Data;

namespace BankOfGraeme.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapGet("/{id:int}", async (int id, AccountService svc) =>
        {
            var account = await svc.GetAccountAsync(id);
            if (account is null) return Results.NotFound();
            var availableBalance = await svc.GetAvailableBalanceAsync(id);
            return Results.Ok(new
            {
                account.Id, account.CustomerId, account.AccountType, account.Bsb,
                account.AccountNumber, account.Name, account.Balance,
                AvailableBalance = availableBalance,
                account.IsActive, account.CreatedAt,
                account.LoanAmount, account.InterestRate, account.LoanTermMonths,
                account.HomeLoanAccountId,
                OffsetAccounts = account.OffsetAccounts.Select(o => new { o.Id, o.Name, o.Balance })
            });
        });

        group.MapGet("/{id:int}/transactions", async (int id, AccountService svc, int page = 1, int pageSize = 20) =>
            Results.Ok(await svc.GetTransactionsAsync(id, page, pageSize)));

        group.MapPost("/{id:int}/deposit", async (int id, DepositRequest req, AccountService svc) =>
        {
            try
            {
                var txn = await svc.DepositAsync(id, req.Amount, req.Description ?? "Deposit");
                return Results.Ok(txn);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:int}/withdraw", async (int id, WithdrawRequest req, AccountService svc) =>
        {
            try
            {
                var txn = await svc.WithdrawAsync(id, req.Amount, req.Description ?? "Withdrawal");
                return Results.Ok(txn);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

public record DepositRequest(decimal Amount, string? Description);
public record WithdrawRequest(decimal Amount, string? Description);
