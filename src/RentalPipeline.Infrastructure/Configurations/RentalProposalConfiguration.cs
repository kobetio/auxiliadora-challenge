using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalPipeline.Domain.Entities;

namespace RentalPipeline.Infrastructure.Configurations;

public class RentalProposalConfiguration : IEntityTypeConfiguration<RentalProposal>
{
    public void Configure(EntityTypeBuilder<RentalProposal> builder)
    {
        builder.ToTable("RentalProposals");

        builder.HasKey(p => p.Id);

        // See PropertyConfiguration for why this is required for client-generated Guid keys.
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.PropertyId)
            .IsRequired();

        builder.Property(p => p.CustomerId)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Optimistic concurrency token mapped to the PostgreSQL `xmin` system column.
        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // References Property/Customer by Id only (aggregate boundary), no navigation back.
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(p => p.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.ProposalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.PropertyId);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.Status);
    }
}
