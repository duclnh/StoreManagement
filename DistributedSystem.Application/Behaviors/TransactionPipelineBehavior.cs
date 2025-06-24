using DistributedSystem.Domain.Abstractions;
using DistributedSystem.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DistributedSystem.Application.Behaviors
{
    public sealed class TransactionPipelineBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork _unitOfWork; //SQL_SERVER_STRATEGY-2
        private readonly ApplicationDbContext _context;//SQL_SERVER_STRATEGY-1

        public TransactionPipelineBehavior(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!IsCommand())
            {
                return await next();
            }

            #region =================SQL_SERVER_STRATEGY-1=====================
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                {
                    var response = await next();

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return response;
                }
            });
            #endregion

            //#region =================SQL_SERVER_STRATEGY-2=====================
            //using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            //{
            //var response = await next();
            //    await _unitOfWork.SaveChangeAsync();
            //    transaction.Complete();
            //    await _unitOfWork.DisposeAsync();
            //    return response;
            //}
            //#endregion
        }
        private bool IsCommand()
            => typeof(TRequest).Name.EndsWith("Command");
    }

}
