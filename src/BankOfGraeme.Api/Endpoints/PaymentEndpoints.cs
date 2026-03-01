using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapGet("/lookup", async (string bsb, string accountNumber, AccountService svc) =>
        {
            var result = await svc.LookupAccountAsync(bsb, accountNumber);
            return result is null ? Results.NotFound(new { error = "No account found for that BSB and account number" }) : Results.Ok(result);
        });

        group.MapPost("/", async (PayRequest req, AccountService svc) =>
        {
            try
            {
                var (from, to) = await svc.PayAsync(req.CallerCustomerId, req.FromAccountId, req.ToBsb, req.ToAccountNumber, req.Amount, req.Description, req.Reference);
                return Results.Ok(new { from, to });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}

public record PayRequest(int CallerCustomerId, int FromAccountId, string ToBsb, string ToAccountNumber, decimal Amount, string? Description, string? Reference);
