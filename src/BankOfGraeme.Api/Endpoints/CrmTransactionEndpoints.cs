using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmTransactionEndpoints
{
    public static void MapCrmTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/accounts/{accountId:int}/transactions")
            .WithTags("CRM Transactions")
            .AddEndpointFilter(StaffAuthFilter);

        group.MapGet("/", async (int accountId, BankDbContext db,
            string? type, DateTime? from, DateTime? to,
            decimal? minAmount, decimal? maxAmount,
            int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.Transactions.Where(t => t.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TransactionType>(type, true, out var txnType))
                query = query.Where(t => t.TransactionType == txnType);

            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            if (minAmount.HasValue)
                query = query.Where(t => Math.Abs(t.Amount) >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(t => Math.Abs(t.Amount) <= maxAmount.Value);

            var total = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new { transactions, total, page, pageSize });
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
