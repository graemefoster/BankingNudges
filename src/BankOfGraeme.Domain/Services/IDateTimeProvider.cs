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
    private readonly BankDbContext _db;
    private int _daysAdvanced;
    private bool _loaded;

    public DatabaseDateTimeProvider(BankDbContext db)
    {
        _db = db;
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

    private void EnsureLoaded()
    {
        if (_loaded) return;
        var setting = _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefault(s => s.Key == "DaysAdvanced");
        _daysAdvanced = setting is not null ? int.Parse(setting.Value) : 0;
        _loaded = true;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        var setting = await _db.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        _daysAdvanced = setting is not null ? int.Parse(setting.Value) : 0;
        _loaded = true;
    }

    public async Task AdvanceDaysAsync(int days)
    {
        EnsureLoaded();
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        if (setting is null)
        {
            setting = new SystemSettings { Key = "DaysAdvanced", Value = "0" };
            _db.SystemSettings.Add(setting);
        }
        _daysAdvanced += days;
        setting.Value = _daysAdvanced.ToString();
        await _db.SaveChangesAsync();
    }

    public async Task ResetAsync()
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "DaysAdvanced");
        if (setting is not null)
        {
            setting.Value = "0";
            await _db.SaveChangesAsync();
        }
        _daysAdvanced = 0;
    }
}
