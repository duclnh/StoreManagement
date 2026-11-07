using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Contract.Services.V1.Identity;
using Identity.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using LoginSessionResponse = Identity.Contract.Services.V1.Identity.Response.LoginSession;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class ListSessionsQueryHandler : IQueryHandler<ListSessions, List<LoginSessionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ListSessionsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<LoginSessionResponse>>> Handle(ListSessions request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var sessions = await _unitOfWork.LoginSessionRepository
                .FindAll(session => session.UserId == request.UserId && !session.IsRevoked && session.ExpiresAtUtc > now)
                .Select(session => new LoginSessionResponse
                {
                    SessionId = session.Id,
                    UserAgent = session.UserAgent,
                    IpAddress = session.IpAddress,
                    CreatedAtUtc = session.CreatedAtUtc.UtcDateTime,
                    LastActiveAtUtc = session.LastActiveAtUtc.UtcDateTime,
                    ExpiresAtUtc = session.ExpiresAtUtc.UtcDateTime,
                    IsCurrent = session.Id == request.CurrentSessionId
                })
                .ToListAsync(cancellationToken);

            return Result.Success(sessions);
        }
    }
}