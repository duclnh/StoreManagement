
using Carter;
using Identity.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using QueryContracts = Identity.Contract.Services.V1.Identity.Query;
using static Identity.Contract.Services.V1.Identity.Command;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Presentation.Apis.V1.Identity
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

            group1.MapPost("register", Register).AllowAnonymous();
            group1.MapPost("login", Authentication).AllowAnonymous();
            group1.MapPost("email/confirmation/request", RequestEmailConfirmation).AllowAnonymous();
            group1.MapPost("email/confirm", ConfirmEmail).AllowAnonymous();
            group1.MapPost("password/forgot", RequestPasswordReset).AllowAnonymous();
            group1.MapPost("password/reset", ResetPassword).AllowAnonymous();
            group1.MapPost("revoke", RevokeToken);
            group1.MapPost("refresh", RefreshToken).AllowAnonymous();
            group1.MapGet("sessions", ListSessions);
            group1.MapPost("sessions/revoke-others", RevokeOtherSessions);
            group1.MapPost("password/change", ChangePassword);
            group1.MapGet("2fa/setup", CreateAuthenticatorSetup);
            group1.MapPost("2fa/enable", EnableTwoFactor);
            group1.MapPost("2fa/disable", DisableTwoFactor);

        }

        private async Task<IResult> Register(ISender sender, [FromBody] Register request)
        {
            var result = await sender.Send(request);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Created($"/api/v1/users/{result.Value}", result);
        }

        private async Task<IResult> RequestEmailConfirmation(
            ISender sender,
            [FromBody] RequestEmailConfirmation request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ConfirmEmail(
            ISender sender,
            [FromBody] ConfirmEmail request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RequestPasswordReset(
            ISender sender,
            [FromBody] RequestPasswordReset request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ResetPassword(
            ISender sender,
            [FromBody] ResetPassword request,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(request, cancellationToken);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> Authentication(ISender sender, HttpContext context, [FromBody] Login login)
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

        private async Task<IResult> RevokeToken(ISender sender, ClaimsPrincipal user, [FromBody] RevokeSession request)
        {
            var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.RevokeToken(userId, request.SessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RefreshToken(ISender sender, [FromBody] RefreshToken request)
        {
            var result = await sender.Send(request);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ListSessions(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetSessionClaims(user, out var userId, out var sessionId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.ListSessions(userId, sessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> RevokeOtherSessions(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetSessionClaims(user, out var userId, out var sessionId))
                return Results.Unauthorized();

            var result = await sender.Send(new QueryContracts.RevokeOtherSessions(userId, sessionId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> ChangePassword(ISender sender, ClaimsPrincipal user, [FromBody] ChangePasswordRequest request)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new ChangePassword(userId, request.CurrentPassword, request.NewPassword));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> CreateAuthenticatorSetup(ISender sender, ClaimsPrincipal user)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new CreateAuthenticatorSetup(userId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> EnableTwoFactor(ISender sender, ClaimsPrincipal user, [FromBody] TwoFactorCode request)
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            var result = await sender.Send(new EnableTwoFactor(userId, request.Code));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        private async Task<IResult> DisableTwoFactor(ISender sender, ClaimsPrincipal user, [FromBody] TwoFactorCode request)
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

