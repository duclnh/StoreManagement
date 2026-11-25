using Identity.Domain.Entities.Identity;
using Xunit;

namespace Identity.Test.Unit;

public sealed class LoginSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rotate_AllowsPreviousTokenOnlyDuringTenSecondGracePeriod()
    {
        var session = CreateSession();
        session.Rotate("new-hash", Now.AddDays(2), Now, "protected-token");

        Assert.True(session.CanRetryRefresh("old-hash", Now.AddSeconds(9)));
        Assert.False(session.CanRetryRefresh("old-hash", Now.AddSeconds(10)));
        Assert.False(session.CanRetryRefresh("wrong-hash", Now));
    }

    [Fact]
    public void Revoke_PreventsRefreshRetryAndFurtherRotation()
    {
        var session = CreateSession();
        session.Rotate("new-hash", Now.AddDays(2), Now, "protected-token");
        session.Revoke(Now);

        Assert.False(session.CanRetryRefresh("old-hash", Now));
        Assert.Throws<InvalidOperationException>(() => session.Rotate("another-hash", Now.AddDays(3), Now));
    }

    [Fact]
    public void RecordActivity_DoesNotChangeRevokedSession()
    {
        var session = CreateSession();
        session.Revoke(Now);
        session.RecordActivity("127.0.0.1", Now.AddMinutes(1));

        Assert.Equal(Now, session.LastActiveAtUtc);
        Assert.Null(session.IpAddress);
    }

    private static LoginSession CreateSession() => new(Guid.NewGuid(), Guid.NewGuid(), "old-hash", null, null, Now, Now.AddDays(1));
}
