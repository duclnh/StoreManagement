using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using MediatR;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.UserCases.V1.Queries.Identity;

public sealed class LogoutSessionQueryHandler(IUnitOfWork unitOfWork, UserManager<AppUser> users) : IQueryHandler<Query.LogoutSession, Unit>
{
    public async Task<Result<Unit>> Handle(Query.LogoutSession request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.LoginSessionRepository.FindByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId || string.IsNullOrWhiteSpace(request.Token)
            || (!LoginSession.HashEquals(session.RefreshTokenHash, GetLoginQueryHandler.HashToken(request.Token))
                && !(session.RefreshRetryUntilUtc > DateTimeOffset.UtcNow
                    && LoginSession.HashEquals(session.PreviousRefreshTokenHash, GetLoginQueryHandler.HashToken(request.Token)))))
            return Result.Failure<Unit>(new Error("Identity.Authentication.InvalidRefreshToken", "Invalid session credentials."));
        if (!session.IsRevoked)
        {
            var user = await users.FindByIdAsync(session.UserId.ToString());
            await unitOfWork.RevokeLoginSessionsAsync(session.UserId, session.Id, cancellationToken: cancellationToken);
            unitOfWork.SecurityAuditRepository.Record(SecurityAuditEventTypes.Logout, SecurityAuditOutcomes.Succeeded,
                session.UserId, "RefreshCredentialLogout", session.Id, user?.TenantId);
            await unitOfWork.SaveChangeAsync(cancellationToken);
        }
        return Result.Success(Unit.Value);
    }
}
