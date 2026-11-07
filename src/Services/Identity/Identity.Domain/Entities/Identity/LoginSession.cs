using Identity.Domain.Abstractions.Entities;

namespace Identity.Domain.Entities.Identity
{
    public class LoginSession : Entity<Guid>
    {
        private LoginSession()
        {
        }

        public LoginSession(
            Guid sessionId,
            Guid userId,
            string refreshTokenHash,
            string? userAgent,
            string? ipAddress,
            DateTimeOffset createdAtUtc,
            DateTimeOffset expiresAtUtc)
        {
            Id = sessionId;
            UserId = userId;
            RefreshTokenHash = refreshTokenHash;
            UserAgent = userAgent;
            IpAddress = ipAddress;
            CreatedAtUtc = createdAtUtc;
            LastActiveAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
        }

        public Guid UserId { get; private set; }
        public string RefreshTokenHash { get; private set; } = string.Empty;
        public string? UserAgent { get; private set; }
        public string? IpAddress { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset LastActiveAtUtc { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; private set; }
        public DateTimeOffset? RevokedAtUtc { get; private set; }
        public bool IsRevoked { get; private set; }
        public byte[] RowVersion { get; private set; } = [];

        public void Rotate(string refreshTokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset lastActiveAtUtc)
        {
            if (IsRevoked)
                throw new InvalidOperationException("A revoked session cannot be refreshed.");

            RefreshTokenHash = refreshTokenHash;
            ExpiresAtUtc = expiresAtUtc;
            LastActiveAtUtc = lastActiveAtUtc;
        }

        public void Revoke(DateTimeOffset revokedAtUtc)
        {
            RevokedAtUtc ??= revokedAtUtc;
            IsRevoked = true;
        }
    }
}