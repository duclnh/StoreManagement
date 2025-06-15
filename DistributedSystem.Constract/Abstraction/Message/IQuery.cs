using DistributedSystem.Constract.Abstraction.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Constract.Abstraction.Message
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
}
