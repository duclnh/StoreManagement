using Identity.Contract.Abstraction.Message;
using MediatR;
using static Identity.Contract.Services.V1.Identity.Response;


namespace Identity.Contract.Services.V1.Identity
{
    public static class Query
    {
        public record Login(string Email, string Password) : IQuery<Authenticated>;
        public record Token(string? AccessToken, string? RefreshToken) : IQuery<Authenticated>;
        public record RefreshToken() : IQuery<Authenticated>;
        public record RevokeToken() : IQuery<Unit>;
    }
}


