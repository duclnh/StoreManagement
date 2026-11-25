using FluentValidation;

namespace Organization.Contract.Services.V1.Warehouses.Validators
{
    public sealed class CreateWarehouseValidator : AbstractValidator<Command.CreateWarehouse>
    {
        public CreateWarehouseValidator()
        {
            RuleFor(x => x.BranchId).NotEmpty();
            RuleFor(x => x.Data).NotNull().SetValidator(new DataValidator());
        }
    }
}
