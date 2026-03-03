using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class ScheduledPaymentEndpoints
{
    public static void MapScheduledPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Scheduled Payments");

        // List scheduled payments for an account
        group.MapGet("/accounts/{accountId:int}/scheduled-payments", async (int accountId, BankDbContext db) =>
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

        // Create a scheduled payment
        group.MapPost("/scheduled-payments", async (CreateScheduledPaymentRequest req, BankDbContext db, IDateTimeProvider dateTime) =>
        {
            var account = await db.Accounts.FindAsync(req.AccountId);
            if (account is null)
                return Results.BadRequest(new { error = "Account not found" });

            if (!account.IsActive)
                return Results.BadRequest(new { error = "Cannot schedule payments on a closed account" });

            if (account.AccountType == AccountType.HomeLoan)
                return Results.BadRequest(new { error = "Cannot schedule payments from a home loan account" });

            if (req.Amount <= 0)
                return Results.BadRequest(new { error = "Amount must be positive" });

            if (!Enum.TryParse<ScheduleFrequency>(req.Frequency, true, out var frequency))
                return Results.BadRequest(new { error = "Invalid frequency. Valid values: OneOff, Weekly, Fortnightly, Monthly, Quarterly, Yearly" });

            if (!DateOnly.TryParse(req.StartDate, out var startDate))
                return Results.BadRequest(new { error = "Invalid start date" });

            DateOnly? endDate = null;
            if (!string.IsNullOrEmpty(req.EndDate))
            {
                if (!DateOnly.TryParse(req.EndDate, out var parsedEnd))
                    return Results.BadRequest(new { error = "Invalid end date" });
                endDate = parsedEnd;
            }

            // Validate payee account exists if specified
            if (req.PayeeAccountId.HasValue)
            {
                var payeeAccount = await db.Accounts.FindAsync(req.PayeeAccountId.Value);
                if (payeeAccount is null || !payeeAccount.IsActive)
                    return Results.BadRequest(new { error = "Payee account not found or inactive" });
            }

            var payment = new ScheduledPayment
            {
                AccountId = req.AccountId,
                PayeeName = req.PayeeName,
                PayeeBsb = req.PayeeBsb,
                PayeeAccountNumber = req.PayeeAccountNumber,
                PayeeAccountId = req.PayeeAccountId,
                Amount = req.Amount,
                Description = req.Description,
                Reference = req.Reference,
                Frequency = frequency,
                StartDate = startDate,
                EndDate = endDate,
                NextDueDate = startDate,
                IsActive = true,
            };

            db.ScheduledPayments.Add(payment);
            await db.SaveChangesAsync();

            return Results.Created($"/api/scheduled-payments/{payment.Id}", new
            {
                payment.Id,
                payment.AccountId,
                payment.PayeeName,
                payment.Amount,
                Frequency = payment.Frequency.ToString(),
                StartDate = payment.StartDate.ToString("yyyy-MM-dd"),
                EndDate = payment.EndDate?.ToString("yyyy-MM-dd"),
                NextDueDate = payment.NextDueDate.ToString("yyyy-MM-dd"),
                payment.IsActive,
            });
        });

        // Update a scheduled payment
        group.MapPut("/scheduled-payments/{id:int}", async (int id, UpdateScheduledPaymentRequest req, BankDbContext db) =>
        {
            var payment = await db.ScheduledPayments.FindAsync(id);
            if (payment is null)
                return Results.NotFound(new { error = "Scheduled payment not found" });

            if (req.Amount.HasValue)
            {
                if (req.Amount.Value <= 0)
                    return Results.BadRequest(new { error = "Amount must be positive" });
                payment.Amount = req.Amount.Value;
            }

            if (req.Frequency is not null)
            {
                if (!Enum.TryParse<ScheduleFrequency>(req.Frequency, true, out var frequency))
                    return Results.BadRequest(new { error = "Invalid frequency" });
                payment.Frequency = frequency;
            }

            if (req.EndDate is not null)
            {
                if (req.EndDate == "")
                    payment.EndDate = null;
                else if (DateOnly.TryParse(req.EndDate, out var parsedEnd))
                    payment.EndDate = parsedEnd;
                else
                    return Results.BadRequest(new { error = "Invalid end date" });
            }

            if (req.Description is not null)
                payment.Description = req.Description;

            if (req.Reference is not null)
                payment.Reference = req.Reference;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                payment.Id,
                payment.AccountId,
                payment.PayeeName,
                payment.Amount,
                Frequency = payment.Frequency.ToString(),
                StartDate = payment.StartDate.ToString("yyyy-MM-dd"),
                EndDate = payment.EndDate?.ToString("yyyy-MM-dd"),
                NextDueDate = payment.NextDueDate.ToString("yyyy-MM-dd"),
                payment.IsActive,
            });
        });

        // Cancel a scheduled payment (soft delete)
        group.MapDelete("/scheduled-payments/{id:int}", async (int id, BankDbContext db) =>
        {
            var payment = await db.ScheduledPayments.FindAsync(id);
            if (payment is null)
                return Results.NotFound(new { error = "Scheduled payment not found" });

            payment.IsActive = false;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Scheduled payment cancelled" });
        });
    }
}

public record CreateScheduledPaymentRequest(
    int AccountId,
    string PayeeName,
    string? PayeeBsb,
    string? PayeeAccountNumber,
    int? PayeeAccountId,
    decimal Amount,
    string? Description,
    string? Reference,
    string Frequency,
    string StartDate,
    string? EndDate);

public record UpdateScheduledPaymentRequest(
    decimal? Amount,
    string? Frequency,
    string? EndDate,
    string? Description,
    string? Reference);
