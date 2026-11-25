using FluentValidation;

namespace Organization.Contract.Services.V1.PointOfSales.Validators
{
    public sealed class CreatePointOfSaleValidator : AbstractValidator<Command.CreatePointOfSale>
    {
        public CreatePointOfSaleValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
