using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class CrmCustomerEndpoints
{
    public static void MapCrmCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/customers")
            .WithTags("CRM Customers")
            .AddEndpointFilter(StaffAuthFilter);

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
                    c.Email.ToLower().Contains(term) ||
                    (c.Phone != null && c.Phone.Contains(term)));
            }

            var total = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id, c.FirstName, c.LastName, c.Email, c.Phone,
                    c.DateOfBirth, c.CreatedAt, c.Persona,
                    FullName = c.FirstName + " " + c.LastName,
                    AccountCount = c.Accounts.Count,
                    ActiveAccountCount = c.Accounts.Count(a => a.IsActive)
                })
                .ToListAsync();

            return Results.Ok(new { customers, total, page, pageSize });
        });

        group.MapGet("/{id:int}", async (int id, BankDbContext db) =>
        {
            var customer = await db.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer is null) return Results.NotFound();

            return Results.Ok(new
            {
                customer.Id, customer.FirstName, customer.LastName,
                customer.Email, customer.Phone, customer.DateOfBirth, customer.CreatedAt,
                customer.Persona,
                FullName = customer.FirstName + " " + customer.LastName,
                Accounts = customer.Accounts.Select(a => new
                {
                    a.Id, a.AccountType, a.Bsb, a.AccountNumber, a.Name,
                    a.Balance, a.IsActive, a.CreatedAt
                })
            });
        });

        group.MapPut("/{id:int}", async (int id, UpdateCustomerRequest req, BankDbContext db) =>
        {
            var customer = await db.Customers.FindAsync(id);
            if (customer is null) return Results.NotFound();

            customer.FirstName = req.FirstName;
            customer.LastName = req.LastName;
            customer.Email = req.Email;
            customer.Phone = req.Phone;
            customer.DateOfBirth = req.DateOfBirth;

            await db.SaveChangesAsync();
            return Results.Ok(customer);
        });
    }

    private static async ValueTask<object?> StaffAuthFilter(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetRequiredService<StaffAuthService>();
        var staffId = auth.GetStaffIdFromContext(context.HttpContext);
        if (staffId is null)
            return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
        return await next(context);
    }
}

public record UpdateCustomerRequest(string FirstName, string LastName, string Email, string? Phone, DateOnly DateOfBirth);
