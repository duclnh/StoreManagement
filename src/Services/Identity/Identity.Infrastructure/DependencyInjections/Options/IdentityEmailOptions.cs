namespace Identity.Infrastructure.DependencyInjections.Options
{
    public sealed class IdentityEmailOptions
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUserName { get; set; }
        public string? SmtpPassword { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "StoreManagement";
        public string EmailConfirmationUrl { get; set; } = string.Empty;
        public string PasswordResetUrl { get; set; } = string.Empty;
    }
}