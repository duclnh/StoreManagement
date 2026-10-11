using Identity.Domain.Abstractions;
using Identity.Persistence;
using MediatR;
using System.Transactions;

namespace Identity.Application.Behaviors
{
    public sealed class TransactionPipelineBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork _unitOfWork; //SQL_SERVER_STRATEGY-2
        public TransactionPipelineBehavior(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!IsCommand())
            {
                return await next();
            }

            //#region =================SQL_SERVER_STRATEGY-2=====================
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            {
                var response = await next();
                await _unitOfWork.SaveChangeAsync();
                transaction.Complete();
                await _unitOfWork.DisposeAsync();
                return response;
            }
            //#endregion
        }
        private bool IsCommand()
            => typeof(TRequest).Name.EndsWith("Command");
    }

}

