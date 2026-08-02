using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Infrastructure.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        // The app always generates the Id client-side (Guid.NewGuid() in the constructor), so EF
        // Core must never assume the database generates it. Without this, an entity discovered via
        // navigation-fixup (rather than an explicit Add) with a non-default Guid key can be
        // misclassified as "already exists" (Modified) instead of "new" (Added) — see
        // ProposalStatusHistoryConfiguration for where this actually surfaced.
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Optimistic concurrency token mapped to the PostgreSQL `xmin` system column.
        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.Status);
    }
}
