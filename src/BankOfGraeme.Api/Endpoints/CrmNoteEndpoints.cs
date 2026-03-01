using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmNoteEndpoints
{
    public static void MapCrmNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/customers/{customerId:int}/notes")
            .WithTags("CRM Notes")
            .AddEndpointFilter(StaffAuthFilter);

        group.MapGet("/", async (int customerId, BankDbContext db) =>
        {
            var notes = await db.CustomerNotes
                .Where(n => n.CustomerId == customerId)
                .Include(n => n.StaffUser)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id, n.Content, n.CreatedAt,
                    Author = n.StaffUser.DisplayName
                })
                .ToListAsync();

            return Results.Ok(notes);
        });

        group.MapPost("/", async (int customerId, AddNoteRequest req, BankDbContext db, HttpContext http) =>
        {
            var staffId = StaffAuthService.GetStaffIdFromContext(http);
            if (staffId is null) return Results.Unauthorized();

            var customer = await db.Customers.FindAsync(customerId);
            if (customer is null) return Results.NotFound();

            var note = new CustomerNote
            {
                CustomerId = customerId,
                StaffUserId = staffId.Value,
                Content = req.Content
            };

            db.CustomerNotes.Add(note);
            await db.SaveChangesAsync();

            var staff = await db.StaffUsers.FindAsync(staffId);
            return Results.Created($"/api/crm/customers/{customerId}/notes", new
            {
                note.Id, note.Content, note.CreatedAt,
                Author = staff?.DisplayName
            });
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

public record AddNoteRequest(string Content);
