namespace ErpClink.Modules.Administration.Domain.Auth;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        string userId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = createdByIp
        };
    }

    public void Revoke(DateTime utcNow, string? revokedByIp, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = utcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenId = replacedByTokenId;
    }
}
