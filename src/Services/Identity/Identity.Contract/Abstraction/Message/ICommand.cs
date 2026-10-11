using Identity.Contract.Abstraction.Shared;
using MassTransit;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Abstraction.Message
{
    [ExcludeFromTopology]
    public interface ICommand : IRequest<Result>
    {
    }

    [ExcludeFromTopology]
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {

    }
}

