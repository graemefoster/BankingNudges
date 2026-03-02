# Copilot Instructions — Bank of Graeme

## Virtual Time Convention (CRITICAL)

**Never use `DateTime.UtcNow`, `DateTime.Now`, `DateTimeOffset.UtcNow`, or `DateTimeOffset.Now` anywhere in the codebase.**

All time must go through the `IDateTimeProvider` interface:

```csharp
// ✅ Correct
public class MyService(IDateTimeProvider dateTime)
{
    public void DoWork()
    {
        var now = dateTime.UtcNow;
        var today = dateTime.Today;
    }
}

// ❌ Wrong — breaks time travel
var now = DateTime.UtcNow;
```

### CreatedAt on entities

Entity `CreatedAt` properties are stamped automatically by the `BankDbContext.SaveChanges` interceptor. Do NOT set `CreatedAt` manually or give it a default value of `DateTime.UtcNow`.

### Where IDateTimeProvider lives

- Interface + implementation: `src/BankOfGraeme.Domain/Services/IDateTimeProvider.cs`
- Registered as `Scoped` in both API and Functions DI containers
- Backed by `SystemSettings` table (`DaysAdvanced` key)

## Project Structure

- `BankOfGraeme.Domain` — shared models, DbContext, services (used by both API and Functions)
- `BankOfGraeme.Api` — minimal API, endpoints, migrations
- `BankOfGraeme.Functions` — Azure Functions nightly interest batch
- `bank-ui` — React customer-facing UI
- `bank-crm` — React staff CRM

## Namespace Convention

The Domain project preserves `BankOfGraeme.Api.Models`, `BankOfGraeme.Api.Data`, and `BankOfGraeme.Api.Services` namespaces (not `BankOfGraeme.Domain.*`). This avoids breaking existing using statements.
