using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Abstraction.Shared
{
    public interface IValidationResult
    {
        public static readonly Error ValidatorError = new(
                "ValidationError",
                "A validation problem occurred"
            );

        Error[] Errors { get; }
        //IDictionary<string, string[]> Errors { get; }
    }
}

