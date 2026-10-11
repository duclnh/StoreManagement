using Identity.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Exceptions
{
    public class ValidationException : DomainException
    {
        public ValidationException(IReadOnlyCollection<ValidationError> errors)
            : base("Validation Failure", "One or more validation erros occurred")
            => Errors = errors;
        
        public IReadOnlyCollection<ValidationError> Errors { get;}

    }

    public record ValidationError(string PropertyName, string ErrorMessage);
}

