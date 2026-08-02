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

        // Nullable: null represents the proposal's initial creation history entry (Rule 8), which
        // has no real "previous" status to record.
        builder.Property(h => h.PreviousStatus)
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
