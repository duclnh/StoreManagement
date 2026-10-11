using Identity.Contract.Abstraction.Message;
using Identity.Persistence;
using Identity.Persistence.Outbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.Infrastructure.BackgroundJobs
{
    [DisallowConcurrentExecution]
    public class ProcessOutboxMessagesJob : IJob
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;

        public ProcessOutboxMessagesJob(ApplicationDbContext dbContext, IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            List<OutboxMessage> outboxMessages = await _dbContext.Set<OutboxMessage>()
                .Where(x => x.ProcessedOnUtc == null)
                .OrderBy(x => x.OccurredOnUtc)
                .Take(20)
                .ToListAsync(context.CancellationToken);

            foreach (OutboxMessage outboxMessage in outboxMessages)
            {
                try
                {
                    IDomainEvent? domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(outboxMessage.Content, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                    });

                    if (domainEvent is null)
                        continue;

                    await _publishEndpoint.Publish(domainEvent, context.CancellationToken);
                    outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    outboxMessage.Error = ex.Message;
                }
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}


