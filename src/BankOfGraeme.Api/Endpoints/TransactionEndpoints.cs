using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Api.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transfers").WithTags("Transfers");

        group.MapPost("/", async (TransferRequest req, AccountService svc) =>
        {
            try
            {
                var (from, to) = await svc.TransferAsync(req.FromAccountId, req.ToAccountId, req.Amount, req.Description);
                return Results.Ok(new { from, to });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount, string? Description);
