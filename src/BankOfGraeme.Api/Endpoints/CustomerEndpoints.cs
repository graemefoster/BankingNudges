using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (BankDbContext db, string? search, int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = db.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(term) ||
                    c.LastName.ToLower().Contains(term) ||
                    c.Email.ToLower().Contains(term));
            }

            var total = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id, c.FirstName, c.LastName, c.Email,
                    FullName = c.FirstName + " " + c.LastName,
                    AccountCount = c.Accounts.Count
                })
                .ToListAsync();

            return Results.Ok(new { customers, total, page, pageSize });
        });

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
