using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Services.V1.Identity.Validators
{
    public class LoginValidator : AbstractValidator<Query.Login>
    {
        public LoginValidator() {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}

