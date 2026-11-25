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
            var count = await unitOfWork.RevokeLoginSessionsAsync(userId, cancellationToken: cancellationToken);

            if (count > 0)
            {
                unitOfWork.SecurityAuditRepository.Record(
                    SecurityAuditEventTypes.SessionsRevoked,
                    SecurityAuditOutcomes.Succeeded,
                    userId,
                    $"AllSessions:{count}");
            }
            await unitOfWork.SaveChangeAsync(cancellationToken);
        }
    }
}