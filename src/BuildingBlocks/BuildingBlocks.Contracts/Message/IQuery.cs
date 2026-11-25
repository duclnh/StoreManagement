using BuildingBlocks.Contracts.Shared;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Message
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
}


