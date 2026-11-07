
using Carter;
using Identity.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using QueryContracts = Identity.Contract.Services.V1.Identity.Query;
using static Identity.Contract.Services.V1.Identity.Command;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Presentation.Apis.Identity
{
    public class AuthApi : ApiEndpoint, ICarterModule
    {
        private const string BaseUrl = "/api/v{version:apiVersion}/auths";

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group1 = app.NewVersionedApi("authentications")
                .MapGroup(BaseUrl)
                .HasApiVersion(1)
                .RequireAuthorization();

            group1.MapPost("register", RegisterV1).AllowAnonymous();
            group1.MapPost("login", AuthenticationV1).AllowAnonymous();
            group1.MapPost("email/confirmation/request", RequestEmailConfirmationV1).AllowAnonymous();
            group1.MapPost("email/confirm", ConfirmEmailV1).AllowAnonymous();
            group1.MapPost("password/forgot", RequestPasswordResetV1).AllowAnonymous();
            group1.MapPost("password/reset", ResetPasswordV1).AllowAnonymous();
            group1.MapPost("revoke", RevokeTokenV1);
            group1.MapPost("refresh", RefreshTokenV1).AllowAnonymous();
            group1.MapGet("sessions", ListSessionsV1);
            group1.MapPost("sessions/revoke-others", RevokeOtherSessionsV1);
            group1.MapPost("password/change", ChangePasswordV1);
            group1.MapGet("2fa/setup", CreateAuthenticatorSetupV1);
            group1.MapPost("2fa/enable", EnableTwoFactorV1);
            group1.MapPost("2fa/disable", DisableTwoFactorV1);

        }

        private async Task<IResult> RegisterV1(ISender sender, [FromBody] Register request)
        {
            var result = await sender.Send(request);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Created($"/api/v1/users/{result.Value}", result);
        }

        private async Task<IResult> RequestEmailConfirmationV1(
            ISender sender,
            [FromBody] RequestEmailConfirmation request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ConfirmEmailV1(
            ISender sender,
            [FromBody] ConfirmEmail request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RequestPasswordResetV1(
            ISender sender,
            [FromBody] RequestPasswordReset request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ResetPasswordV1(
            ISender sender,
            [FromBody] ResetPassword request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> AuthenticationV1(ISender sender, HttpContext context, [FromBody] Login login)
        {
            var request = new Login(
                login.Email,
                login.Password,
                context.Request.Headers.UserAgent.ToString(),
                context.Connection.RemoteIpAddress?.ToString(),
                login.TwoFactorCode);
            var result = await sender.Send(request);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RevokeTokenV1(ISender sender, ClaimsPrincipal user, [FromBody] RevokeSession request)
        {
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.RevokeToken(userId, request.SessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RefreshTokenV1(ISender sender, [FromBody] RefreshToken request)
        {
            var result = await sender.Send(request);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ListSessionsV1(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetSessionClaims(user, out var userId, out var sessionId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.ListSessions(userId, sessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RevokeOtherSessionsV1(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetSessionClaims(user, out var userId, out var sessionId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.RevokeOtherSessions(userId, sessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ChangePasswordV1(ISender sender, ClaimsPrincipal user, [FromBody] ChangePasswordRequest request)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new ChangePassword(userId, request.CurrentPassword, request.NewPassword));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> CreateAuthenticatorSetupV1(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new CreateAuthenticatorSetup(userId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> EnableTwoFactorV1(ISender sender, ClaimsPrincipal user, [FromBody] TwoFactorCode request)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new EnableTwoFactor(userId, request.Code));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> DisableTwoFactorV1(ISender sender, ClaimsPrincipal user, [FromBody] TwoFactorCode request)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new DisableTwoFactor(userId, request.Code));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

        private static bool TryGetSessionClaims(ClaimsPrincipal user, out Guid userId, out Guid sessionId)
        {
            var hasUserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
            var hasSessionId = Guid.TryParse(user.FindFirstValue(ClaimTypes.Sid), out sessionId);
            return hasUserId && hasSessionId;
        }
    }
}


