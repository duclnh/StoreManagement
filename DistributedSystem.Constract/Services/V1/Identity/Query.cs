using DistributedSystem.Constract.Abstraction.Message;
using MediatR;
using static DistributedSystem.Constract.Services.V1.Identity.Response;


namespace DistributedSystem.Constract.Services.V1.Identity
{
    public static class Query
    {
        public record Login(string Email, string Password) : IQuery<Authenticated>;
        public record Token(string? AccessToken, string? RefreshToken) : IQuery<Authenticated>;
        public record RefreshToken() : IQuery<Authenticated>;
        public record RevokeToken() : IQuery<Unit>;
    }
}
