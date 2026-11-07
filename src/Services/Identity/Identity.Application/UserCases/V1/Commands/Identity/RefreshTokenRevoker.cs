using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    internal static class RefreshTokenRevoker
    {
        internal static async Task RevokeAllAsync(
            IUnitOfWork unitOfWork,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sessions = await unitOfWork.LoginSessionRepository
                .FindAll(session => session.UserId == userId && !session.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.Revoke(DateTimeOffset.UtcNow);
                unitOfWork.LoginSessionRepository.Update(session);
            }

            if (sessions.Count > 0)
            {
                unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.SessionsRevoked,
                    SecurityAuditOutcomes.Succeeded,
                    userId,
                    $"AllSessions:{sessions.Count}");
            }
            await unitOfWork.SaveChangeAsync(cancellationToken);
        }
    }
}