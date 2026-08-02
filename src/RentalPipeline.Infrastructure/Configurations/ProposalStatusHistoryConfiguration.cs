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

        // Critical for this entity specifically: history rows are added to an already-tracked
        // RentalProposal's StatusHistory collection (not via an explicit repository Add()), so
        // without this, EF Core's change tracker misclassifies each new row as an update against a
        // non-existent row (DbUpdateConcurrencyException: "expected to affect 1 row(s), but
        // actually affected 0"). See PropertyConfiguration for the full explanation.
        builder.Property(h => h.Id)
            .ValueGeneratedNever();

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
