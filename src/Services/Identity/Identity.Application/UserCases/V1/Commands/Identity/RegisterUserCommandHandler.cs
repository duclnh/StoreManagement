using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Identity.Application.Abstractions;
using Identity.Domain.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public class RegisterUserCommandHandler : ICommandHandler<Register, Guid>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IIdentityEmailSender _emailSender;
        private readonly ILogger<RegisterUserCommandHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterUserCommandHandler(
            UserManager<AppUser> userManager,
            IIdentityEmailSender emailSender,
            ILogger<RegisterUserCommandHandler> logger,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(Register request, CancellationToken cancellationToken)
        {
            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                TenantId = TenantDefaults.LegacyTenantId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {request.LastName}".Trim()
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(error => error.Description));
                return Result.Failure<Guid>(new Error("Identity.Registration.Failed", message));
            }

            _unitOfWork.SecurityAuditRepository.Record(
                SecurityAuditEventTypes.UserCreated,
                SecurityAuditOutcomes.Succeeded,
                user.Id,
                "SelfRegistration",
                tenantId: user.TenantId);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailSender.SendEmailConfirmationAsync(user.Email!, token, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Could not send the email confirmation message for user {UserId}.", user.Id);
            }

            return Result.Success(user.Id);
        }
    }
}