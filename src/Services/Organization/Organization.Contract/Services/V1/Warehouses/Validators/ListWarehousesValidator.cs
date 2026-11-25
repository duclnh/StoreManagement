using FluentValidation;

namespace Organization.Contract.Services.V1.Warehouses.Validators
{
    public sealed class ListWarehousesValidator : AbstractValidator<Query.ListWarehouses>
    {
        public ListWarehousesValidator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(150);
        }
    }
}
