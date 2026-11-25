using FluentValidation;

namespace Organization.Contract.Services.V1.PointOfSales.Validators
{
    public sealed class ListPointOfSalesValidator : AbstractValidator<Query.ListPointOfSales>
    {
        public ListPointOfSalesValidator()
        {
            RuleFor(x => x.Page).InclusiveBetween(1, 100000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Search).MaximumLength(150);
        }
    }
}
