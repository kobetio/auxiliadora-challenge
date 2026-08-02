using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Infrastructure.Configurations;

public class ProposalStatusHistoryConfiguration : IEntityTypeConfiguration<ProposalStatusHistory>
{
    public void Configure(EntityTypeBuilder<ProposalStatusHistory> builder)
    {
        builder.ToTable("ProposalStatusHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ProposalId)
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(h => h.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.HasIndex(h => h.ProposalId);
    }
}
