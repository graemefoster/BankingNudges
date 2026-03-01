using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Services;

public class StaffAuthService(BankDbContext db)
{
    private static readonly ConcurrentDictionary<string, (int StaffId, string Username, string DisplayName, string Role, DateTime ExpiresAt)> Tokens = new();
    private static readonly ConcurrentDictionary<int, string> StaffTokens = new(); // staffId → current token (for cleanup)

    public async Task<object?> LoginAsync(string username, string password)
    {
        var staff = await db.StaffUsers.FirstOrDefaultAsync(s => s.Username == username);
        if (staff is null || !BCrypt.Net.BCrypt.Verify(password, staff.PasswordHash))
            return null;

        // Remove old token for this staff user
        if (StaffTokens.TryRemove(staff.Id, out var oldToken))
            Tokens.TryRemove(oldToken, out _);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Tokens[token] = (staff.Id, staff.Username, staff.DisplayName, staff.Role, DateTime.UtcNow.AddHours(8));
        StaffTokens[staff.Id] = token;

        return new { token, staff.Username, staff.DisplayName, staff.Role };
    }

    public static (int StaffId, string Username, string DisplayName, string Role)? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (Tokens.TryGetValue(token, out var session))
        {
            if (session.ExpiresAt > DateTime.UtcNow)
                return (session.StaffId, session.Username, session.DisplayName, session.Role);
            Tokens.TryRemove(token, out _);
        }

        return null;
    }

    public static int? GetStaffIdFromContext(HttpContext context)
    {
        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var session = ValidateToken(token);
        return session?.StaffId;
    }
}
