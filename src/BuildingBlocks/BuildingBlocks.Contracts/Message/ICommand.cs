using BuildingBlocks.Contracts.Shared;

using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Contracts.Message
{
    public interface ICommandMarker { }

    public interface ICommand : IRequest<Result>, ICommandMarker
    {
    }

    public interface ICommand<TResponse> : IRequest<Result<TResponse>>, ICommandMarker
    {

    }
}

