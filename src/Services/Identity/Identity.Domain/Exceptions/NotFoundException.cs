using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Exceptions
{
    public abstract class NotFoundException: DomainException
    {
        protected NotFoundException(string message) : base("Not found", message)
        {
            
        }
    }
}

