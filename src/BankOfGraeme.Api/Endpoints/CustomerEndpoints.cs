using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (BankDbContext db) =>
            await db.Customers.Select(c => new
            {
                c.Id, c.FirstName, c.LastName, c.Email,
                FullName = c.FirstName + " " + c.LastName,
                AccountCount = c.Accounts.Count
            }).ToListAsync());

        group.MapGet("/{id:int}", async (int id, BankDbContext db) =>
        {
            var customer = await db.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        });

        group.MapGet("/{id:int}/accounts", async (int id, AccountService svc) =>
            Results.Ok(await svc.GetCustomerAccountsAsync(id)));
    }
}
