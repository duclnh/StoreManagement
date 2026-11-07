using Identity.Domain.Entities.Identity;
using Identity.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Persistence.Configurations
{
    public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
    {
        public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
        {
            builder.ToTable(TableNames.SecurityAuditEvents);
            builder.HasKey(auditEvent => auditEvent.Id);
            builder.Property(auditEvent => auditEvent.EventType).HasMaxLength(100).IsRequired();
            builder.Property(auditEvent => auditEvent.Outcome).HasMaxLength(32).IsRequired();
            builder.Property(auditEvent => auditEvent.OccurredAtUtc).IsRequired();
            builder.Property(auditEvent => auditEvent.IpAddress).HasMaxLength(45);
            builder.Property(auditEvent => auditEvent.Details).HasMaxLength(256);
            builder.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
            builder.HasIndex(auditEvent => new { auditEvent.ActorUserId, auditEvent.OccurredAtUtc });
            builder.HasIndex(auditEvent => new { auditEvent.SubjectUserId, auditEvent.OccurredAtUtc });
            builder.HasIndex(auditEvent => new { auditEvent.TenantId, auditEvent.OccurredAtUtc });
            builder.HasIndex(auditEvent => new { auditEvent.EventType, auditEvent.OccurredAtUtc });
        }
    }
}
