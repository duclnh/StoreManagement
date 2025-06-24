using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Constract.Services.V1.Product.Validators
{
    public class CreateProductValidator : AbstractValidator<Command.CreateProductCommand>
    {
        public CreateProductValidator() {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Description).NotEmpty();
        }
    }
}
