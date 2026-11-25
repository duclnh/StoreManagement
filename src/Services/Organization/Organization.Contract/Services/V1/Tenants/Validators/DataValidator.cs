using System.Globalization;
using System.Text.Json;
using FluentValidation;

namespace Organization.Contract.Services.V1.Tenants.Validators
{
    public sealed class DataValidator : AbstractValidator<Command.Data>
    {
        public DataValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.ContactEmail).MaximumLength(254);
            RuleFor(x => x.ContactEmail).Must(x => string.IsNullOrWhiteSpace(x) || System.Net.Mail.MailAddress.TryCreate(x, out _));
            RuleFor(x => x.ContactPhone).MaximumLength(32);
            RuleFor(x => x.ConfigurationJson).NotEmpty().MaximumLength(65536).Must(IsJsonObject);
        }

        private static bool IsJsonObject(string? value)
        {
            try
            {
                using var document = JsonDocument.Parse(value ?? "");
                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IsTimeZone(string? value) => value is not null && TimeZoneInfo.TryFindSystemTimeZoneById(value, out _);

        private static bool IsLanguage(string? value)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(value) && CultureInfo.GetCultureInfo(value).Name.Length > 0;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
