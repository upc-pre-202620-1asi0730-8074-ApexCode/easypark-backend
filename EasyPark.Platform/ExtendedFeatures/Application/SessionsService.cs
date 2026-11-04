using System.Security.Cryptography;
using System.Text;
using EasyPark.Platform.ExtendedFeatures.Domain.Model;
using EasyPark.Platform.Iam.Application.Internal.OutboundServices;
using EasyPark.Platform.Iam.Domain.Model.Aggregates;
using EasyPark.Platform.Shared.Domain.Model.Exceptions;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace EasyPark.Platform.ExtendedFeatures.Application;

public record RefreshTokenIssued(string RefreshToken, DateTime ExpiresAt);
public record RefreshedSession(string Token, string RefreshToken, DateTime RefreshExpiresAt);
public record SessionInfo(int Id, DateTime CreatedAt, DateTime ExpiresAt, DateTime? RevokedAt);

/// <summary>Opaque, rotating refresh tokens: only their SHA-256 hashes are stored in MySQL.</summary>
public sealed class SessionsService(AppDbContext db, ITokenService tokenService)
{
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public async Task<RefreshTokenIssued> IssueAsync(int userId, CancellationToken ct)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var row = new RefreshToken { UserAccountId = userId, TokenHash = Hash(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(30) };
        db.Set<RefreshToken>().Add(row); await db.SaveChangesAsync(ct);
        return new RefreshTokenIssued(raw, row.ExpiresAt);
    }

    public async Task<RefreshedSession> RefreshAsync(string? raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 512)
            throw DomainException.Unauthorized("invalid-refresh-token", "Invalid refresh token.");
        var hash = Hash(raw);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var record = await db.Set<RefreshToken>().AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (record is null || record.RevokedAt.HasValue || record.ExpiresAt <= DateTime.UtcNow)
            throw DomainException.Unauthorized("invalid-refresh-token", "Refresh token is expired or revoked.");
        var account = await db.Set<UserAccount>().FirstOrDefaultAsync(x => x.Id == record.UserAccountId, ct);
        if (account is null || !account.IsActive)
            throw DomainException.Unauthorized("invalid-refresh-token", "Account is no longer active.");
        // Atomic check-and-revoke prevents two concurrent refreshes from reusing the same token.
        var changed = await db.Set<RefreshToken>().Where(x => x.Id == record.Id && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        if (changed != 1)
            throw DomainException.Unauthorized("invalid-refresh-token", "Refresh token was already used.");
        var renewed = await IssueAsync(account.Id, ct);
        await transaction.CommitAsync(ct);
        return new RefreshedSession(tokenService.GenerateToken(account), renewed.RefreshToken, renewed.ExpiresAt);
    }

    public async Task RevokeAsync(int userId, string? raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 512)
            throw DomainException.BadRequest("invalid-refresh-token", "Refresh token required.");
        var hash = Hash(raw);
        var affected = await db.Set<RefreshToken>().Where(x => x.UserAccountId == userId &&
            x.TokenHash == hash && x.RevokedAt == null).ExecuteUpdateAsync(
                x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        if (affected == 0) throw DomainException.NotFound("session-not-found", "Active session not found.");
    }

    public async Task RevokeAllAsync(int userId, CancellationToken ct)
    {
        await db.Set<RefreshToken>().Where(x => x.UserAccountId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeByIdAsync(int userId, int id, CancellationToken ct)
    {
        var affected = await db.Set<RefreshToken>().Where(x => x.UserAccountId == userId && x.Id == id && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
        if (affected == 0) throw DomainException.NotFound("session-not-found", "Active session not found.");
    }

    public async Task<IReadOnlyList<SessionInfo>> ListAsync(int userId, CancellationToken ct) =>
        await db.Set<RefreshToken>().AsNoTracking().Where(x => x.UserAccountId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SessionInfo(x.Id, x.CreatedAt, x.ExpiresAt, x.RevokedAt))
            .ToListAsync(ct);
}
