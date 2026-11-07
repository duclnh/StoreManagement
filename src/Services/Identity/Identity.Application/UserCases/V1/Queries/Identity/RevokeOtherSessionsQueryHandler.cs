using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class RevokeOtherSessionsQueryHandler : IQueryHandler<RevokeOtherSessions, int>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RevokeOtherSessionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(RevokeOtherSessions request, CancellationToken cancellationToken)
        {
            var sessions = await _unitOfWork.LoginSessionRepository
                .FindAll(session => session.UserId == request.UserId
                    && !session.IsRevoked
                    && session.Id != request.CurrentSessionId)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.Revoke(DateTimeOffset.UtcNow);
                _unitOfWork.LoginSessionRepository.Update(session);
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result.Success(sessions.Count);
        }
    }
}