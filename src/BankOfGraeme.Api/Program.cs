using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Endpoints;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("BankOfGraeme.Api")));

builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<StaffAuthService>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    db.Database.Migrate();
    SeedData.Seed(db);
}

app.UseCors();
app.MapOpenApi();

app.MapCustomerEndpoints();
app.MapAccountEndpoints();
app.MapTransactionEndpoints();
app.MapPaymentEndpoints();

// CRM endpoints
app.MapCrmAuthEndpoints();
app.MapCrmCustomerEndpoints();
app.MapCrmAccountEndpoints();
app.MapCrmNoteEndpoints();
app.MapCrmTransactionEndpoints();

app.Run();
