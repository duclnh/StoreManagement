using FluentValidation;

namespace Organization.Contract.Services.V1.PointOfSales.Validators
{
    public sealed class UpdatePointOfSaleValidator : AbstractValidator<Command.UpdatePointOfSale>
    {
        public UpdatePointOfSaleValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
