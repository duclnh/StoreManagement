using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Abstractions;
using Identity.Domain.Entities.Identity;
using MediatR;
using static Identity.Contract.Services.V1.Identity.Query;

namespace Identity.Application.UserCases.V1.Queries.Identity
{
    public class RevokeSessionQueryHandler : IQueryHandler<RevokeToken, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RevokeSessionQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Unit>> Handle(RevokeToken request, CancellationToken cancellationToken)
        {
            var session = await _unitOfWork.LoginSessionRepository.FindByIdAsync(request.SessionId, cancellationToken);
            if (session is null || session.UserId != request.UserId)
                return Result.Failure<Unit>(new Error("Identity.Authentication.SessionNotFound", "The login session was not found."));

            session.Revoke(DateTimeOffset.UtcNow);
            _unitOfWork.LoginSessionRepository.Update(session);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result.Success(Unit.Value);
        }
    }
}