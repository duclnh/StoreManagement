using Identity.Application.Abstractions;
using Identity.Infrastructure.DependencyInjections.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;

namespace Identity.Infrastructure.Email
{
    public sealed class SmtpIdentityEmailSender : IIdentityEmailSender
    {
        private readonly IdentityEmailOptions _options;

        public SmtpIdentityEmailSender(IOptions<IdentityEmailOptions> options)
        {
            _options = options.Value;
        }

        public Task SendEmailConfirmationAsync(string email, string token, CancellationToken cancellationToken)
        {
            var confirmationUrl = AddQueryParameters(_options.EmailConfirmationUrl, email, token);
            var encodedUrl = WebUtility.HtmlEncode(confirmationUrl);
            return SendAsync(
                email,
                "Confirm your email",
                $"<p>Confirm your email address by opening <a href=\"{encodedUrl}\">this link</a>.</p>",
                $"Confirm your email address: {confirmationUrl}",
                cancellationToken);
        }

        public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
        {
            var resetUrl = AddQueryParameters(_options.PasswordResetUrl, email, token);
            var encodedUrl = WebUtility.HtmlEncode(resetUrl);
            return SendAsync(
                email,
                "Reset your password",
                $"<p>Reset your password by opening <a href=\"{encodedUrl}\">this link</a>.</p>",
                $"Reset your password: {resetUrl}",
                cancellationToken);
        }

        private async Task SendAsync(
            string recipient,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost)
                || string.IsNullOrWhiteSpace(_options.FromEmail))
            {
                throw new InvalidOperationException("Identity email delivery is not configured.");
            }

            if (!string.IsNullOrWhiteSpace(_options.SmtpUserName)
                && string.IsNullOrWhiteSpace(_options.SmtpPassword))
            {
                throw new InvalidOperationException("SMTP password is required when SMTP authentication is configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.SmtpUserName))
                await client.AuthenticateAsync(_options.SmtpUserName, _options.SmtpPassword!, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        private static string AddQueryParameters(string baseUrl, string email, string token)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps
                    && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            {
                throw new InvalidOperationException("Identity email callback URLs must use HTTPS, except for loopback development URLs.");
            }

            var builder = new UriBuilder(uri);
            var existingQuery = builder.Query.TrimStart('?');
            var tokenQuery = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            builder.Query = string.IsNullOrEmpty(existingQuery) ? tokenQuery : $"{existingQuery}&{tokenQuery}";
            return builder.Uri.ToString();
        }
    }
}