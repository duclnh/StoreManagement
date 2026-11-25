using System.Globalization;
using System.Text.Json;
using FluentValidation;

namespace Organization.Contract.Services.V1.Organizations.Validators
{
    public sealed class DataValidator : AbstractValidator<Command.Data>
    {
        public DataValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.TaxCode).MaximumLength(500);
            RuleFor(x => x.BusinessType).Must(x => new[] { "PHONE_STORE", "ELECTRONICS", "FASHION", "COSMETICS", "GROCERY", "FURNITURE", "GENERAL_RETAIL" }.Contains(x));
            RuleFor(x => x.Industry).MaximumLength(500);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.Phone).MaximumLength(32);
            RuleFor(x => x.Email).MaximumLength(254);
            RuleFor(x => x.Email).Must(x => string.IsNullOrWhiteSpace(x) || System.Net.Mail.MailAddress.TryCreate(x, out _));
            RuleFor(x => x.LogoUrl).MaximumLength(2048);
            RuleFor(x => x.Currency).NotEmpty().Matches("^[A-Z]{3}$");
            RuleFor(x => x.TimeZone).NotEmpty().Must(IsTimeZone);
            RuleFor(x => x.Language).NotEmpty().MaximumLength(20).Must(IsLanguage);
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
