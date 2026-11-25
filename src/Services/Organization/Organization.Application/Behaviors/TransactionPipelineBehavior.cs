using BuildingBlocks.Contracts.Message;
using BuildingBlocks.Contracts.Shared;
using MediatR;
using Organization.Domain.Abstractions;

namespace Organization.Application.Behaviors
{
    public sealed class TransactionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICommandTransaction _transaction;

        public TransactionPipelineBehavior(ICommandTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return request is ICommandMarker
                ? _transaction.ExecuteAsync(_ => next(), response => response is Result { IsSuccess: true }, cancellationToken)
                : next();
        }
    }
}
