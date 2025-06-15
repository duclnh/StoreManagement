using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DistributedSystem.Constract.Abstraction.Shared
{
    public sealed class ValidationResult : Result, IValidationResult
    {
        //private ValidationResult(Error[] errors) : base(false, IValidationResult.ValidatorError) =>
        //    Errors = errors;

        //public Error[] Errors;

        //public static ValidationResult WithErros(Error[] errors) => new(errors);

        private ValidationResult(IDictionary<string, string[]> errors) : base(false, IValidationResult.ValidatorError) =>
            Errors = errors;
        public IDictionary<string, string[]> Errors { get; }

        public static ValidationResult WithErrors(IDictionary<string, string[]> errors) => new(errors);
    }
}
