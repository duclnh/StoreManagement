using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Abstraction.Shared
{
    public sealed class ValidationResult<TValue> : Result<TValue>, IValidationResult
    {
        //private ValidationResult(IDictionary<string, string[]> errors) : base(default, false, IValidationResult.ValidatorError)
        //    => Errors = errors;

        //public IDictionary<string, string[]> Errors { get; }

        //public static ValidationResult<TValue> WithErrors(IDictionary<string, string[]> errors) => new(errors);

        private ValidationResult(Error[] errors) : base(default, false, IValidationResult.ValidatorError)
            => Errors = errors;

        public Error[] Errors { get; }

        public static ValidationResult<TValue> WithErrors(Error[] errors) => new(errors);
    }
}

