using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Identity.Contract.Abstraction.Shared
{
    public sealed class ValidationResult : Result, IValidationResult
    {
        private ValidationResult(Error[] errors) : base(false, IValidationResult.ValidatorError) =>
            Errors = errors;

        public Error[] Errors { get; }

        public static ValidationResult WithErrors(Error[] errors) => new(errors);

        //private ValidationResult(IDictionary<string, string[]> errors) : base(false, IValidationResult.ValidatorError) =>
        //    Errors = errors;
        //public IDictionary<string, string[]> Errors { get; }

        //public static ValidationResult WithErrors(IDictionary<string, string[]> errors) => new(errors);
    }
}


