using Identity.Domain.Abstractions.Entities;

namespace Identity.Domain.Entities.Identity
{
    public class LoginSession : Entity<Guid>
    {
        protected LoginSession()
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
        public string? PreviousRefreshTokenHash { get; private set; }
        public string? ProtectedRefreshToken { get; private set; }
        public DateTimeOffset? RefreshRetryUntilUtc { get; private set; }
        public string? UserAgent { get; private set; }
        public string? IpAddress { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset LastActiveAtUtc { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; private set; }
        public DateTimeOffset? RevokedAtUtc { get; private set; }
        public bool IsRevoked { get; private set; }
        public byte[] RowVersion { get; private set; } = [];

        public void Rotate(string refreshTokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset lastActiveAtUtc, string? protectedToken = null)
        {
            if (IsRevoked)
                throw new InvalidOperationException("A revoked session cannot be refreshed.");

            PreviousRefreshTokenHash = RefreshTokenHash;
            ProtectedRefreshToken = protectedToken;
            RefreshRetryUntilUtc = lastActiveAtUtc.AddSeconds(10);
            RefreshTokenHash = refreshTokenHash;
            ExpiresAtUtc = expiresAtUtc;
            LastActiveAtUtc = lastActiveAtUtc;
        }

        public bool CanRetryRefresh(string hash, DateTimeOffset now) =>
            !IsRevoked && ExpiresAtUtc > now && RefreshRetryUntilUtc > now
            && ProtectedRefreshToken is not null && HashEquals(PreviousRefreshTokenHash, hash);

        public static bool HashEquals(string? expected, string supplied) => expected is not null
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(expected), System.Text.Encoding.ASCII.GetBytes(supplied));

        public void Revoke(DateTimeOffset revokedAtUtc)
        {
            RevokedAtUtc ??= revokedAtUtc;
            IsRevoked = true;
        }

        public void RecordActivity(string? ipAddress, DateTimeOffset now)
        {
            if (IsRevoked) return;
            LastActiveAtUtc = now;
            if (!string.IsNullOrWhiteSpace(ipAddress)) IpAddress = ipAddress;
        }
    }
}
