using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
    int DaysAdvanced { get; }
    Task AdvanceDaysAsync(int days);
    Task ResetAsync();
}

public class DatabaseDateTimeProvider : IDateTimeProvider
{
    private readonly DbContextOptions<BankDbContext> _options;
    private int _daysAdvanced;
    private bool _loaded;

    public DatabaseDateTimeProvider(DbContextOptions<BankDbContext> options)
    {
        _options = options;
    }

    public DateTime UtcNow
    {
        get
        {
            EnsureLoaded();
            return DateTime.UtcNow.AddDays(_daysAdvanced);
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    public int DaysAdvanced
    {
        get
        {
            EnsureLoaded();
            return _daysAdvanced;
        }
    }

    private BankDbContext CreateDb() => new(_options);

    private void EnsureLoaded()
    {
        if (_loaded) return;
        using var db = CreateDb();
        var setting = db.SystemSettings
            .AsNoTracking()
            .FirstOrDefault(s => s.Key == "DaysAdvanced");
        _daysAdvanced = setting is not null ? int.Parse(setting.Value) : 0;
        _loaded = true;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        using var db = CreateDb();
        var setting = await db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        _daysAdvanced = setting is not null ? int.Parse(setting.Value) : 0;
        _loaded = true;
    }

    public async Task AdvanceDaysAsync(int days)
    {
        EnsureLoaded();
        using var db = CreateDb();
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        if (setting is null)
        {
            setting = new SystemSettings { Key = "DaysAdvanced", Value = "0" };
            db.SystemSettings.Add(setting);
        }
        _daysAdvanced += days;
        setting.Value = _daysAdvanced.ToString();
        await db.SaveChangesAsync();
    }

    public async Task ResetAsync()
    {
        using var db = CreateDb();
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        if (setting is not null)
        {
            setting.Value = "0";
        }

        // Also clear LastProcessedDate so the catch-up function re-processes from today
        var lastProcessed = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LastProcessedDate");
        if (lastProcessed is not null)
        {
            db.SystemSettings.Remove(lastProcessed);
        }

        await db.SaveChangesAsync();
        _daysAdvanced = 0;
    }
}
