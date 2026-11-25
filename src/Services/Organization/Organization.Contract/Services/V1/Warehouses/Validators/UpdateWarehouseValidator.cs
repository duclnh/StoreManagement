using FluentValidation;

namespace Organization.Contract.Services.V1.Warehouses.Validators
{
    public sealed class UpdateWarehouseValidator : AbstractValidator<Command.UpdateWarehouse>
    {
        public UpdateWarehouseValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
