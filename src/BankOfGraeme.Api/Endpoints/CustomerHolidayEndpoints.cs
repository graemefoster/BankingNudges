using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CustomerHolidayEndpoints
{
    public static void MapCustomerHolidayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers/{customerId:int}/holidays")
            .WithTags("Customer Holidays");

        group.MapGet("/", async (int customerId, BankDbContext db) =>
        {
            var holidays = await db.CustomerHolidays
                .AsNoTracking()
                .Where(h => h.CustomerId == customerId)
                .OrderByDescending(h => h.StartDate)
                .Select(h => new
                {
                    h.Id, h.Destination, h.StartDate, h.EndDate, h.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(holidays);
        });

        group.MapGet("/active", async (int customerId, BankDbContext db, IDateTimeProvider dateTime) =>
        {
            var today = dateTime.Today;
            var active = await db.CustomerHolidays
                .AsNoTracking()
                .Where(h => h.CustomerId == customerId && h.StartDate <= today && h.EndDate >= today)
                .Select(h => new { h.Id, h.Destination, h.StartDate, h.EndDate })
                .FirstOrDefaultAsync();

            return active is not null ? Results.Ok(active) : Results.NoContent();
        });

        group.MapPost("/", async (int customerId, RegisterHolidayRequest req, BankDbContext db, IDateTimeProvider dateTime) =>
        {
            var customer = await db.Customers.FindAsync(customerId);
            if (customer is null)
                return Results.NotFound(new { error = "Customer not found" });

            if (req.EndDate < req.StartDate)
                return Results.BadRequest(new { error = "End date must be on or after start date" });

            var holiday = new CustomerHoliday
            {
                CustomerId = customerId,
                Destination = req.Destination,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
            };

            db.CustomerHolidays.Add(holiday);
            await db.SaveChangesAsync();

            return Results.Created($"/api/customers/{customerId}/holidays/{holiday.Id}", new
            {
                holiday.Id, holiday.Destination, holiday.StartDate, holiday.EndDate, holiday.CreatedAt
            });
        });
    }

    private record RegisterHolidayRequest(string Destination, DateOnly StartDate, DateOnly EndDate);
}
