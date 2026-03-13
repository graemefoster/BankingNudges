using Azure.Identity;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Endpoints;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel.Primitives;
using System.Text.Json.Serialization;

#pragma warning disable OPENAI001 // Experimental API

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("BankOfGraeme.Api")));

builder.Services.AddScoped<IDateTimeProvider, DatabaseDateTimeProvider>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<StaffAuthService>();
builder.Services.AddScoped<NudgePatternDetector>();
builder.Services.AddScoped<NudgeSignalDetector>();
builder.Services.AddScoped<NudgeContextAssembler>();
builder.Services.AddScoped<NudgeGenerator>();
builder.Services.AddSingleton(new NudgeGeneratorSettings(
    builder.Configuration["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4o"));
builder.Services.AddScoped<NudgeBatchRunner>();
builder.Services.AddScoped<NudgeChatAgent>();
builder.Services.AddScoped<NudgeChatTools>();

builder.Services.AddSingleton<ResponsesClient>(_ =>
{
    var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"]
        ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required");

    return new ResponsesClient(
        new BearerTokenPolicy(new DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default"),
        new OpenAIClientOptions { Endpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1/") });
});

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
    var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
    SeedData.Seed(db, dateTime);
}

app.UseCors();
app.MapOpenApi();

app.MapCustomerEndpoints();
app.MapCustomerHolidayEndpoints();
app.MapAccountEndpoints();
app.MapTransactionEndpoints();
app.MapPaymentEndpoints();
app.MapScheduledPaymentEndpoints();

// CRM endpoints
app.MapCrmAuthEndpoints();
app.MapCrmCustomerEndpoints();
app.MapCrmAccountEndpoints();
app.MapCrmNoteEndpoints();
app.MapCrmTransactionEndpoints();
app.MapCrmScheduledPaymentEndpoints();

// Time travel
app.MapTimeTravelEndpoints();

// Nudges
app.MapNudgeEndpoints();

// Nudge chat
app.MapChatEndpoints();

app.Run();
