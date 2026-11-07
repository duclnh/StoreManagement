namespace Identity.Application.Abstractions
{
    public interface IIdentityEmailSender
    {
        Task SendEmailConfirmationAsync(string email, string token, CancellationToken cancellationToken);
        Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken);
    }
}