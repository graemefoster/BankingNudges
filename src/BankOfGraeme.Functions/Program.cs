using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"]));

builder.Services.AddScoped<IDateTimeProvider, DatabaseDateTimeProvider>();
builder.Services.AddScoped<InterestCalculationService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<ScheduledPaymentService>();

builder.Build().Run();
