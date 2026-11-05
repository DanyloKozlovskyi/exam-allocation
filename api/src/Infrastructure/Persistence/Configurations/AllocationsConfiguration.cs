using Domain.Allocations;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AllocationsConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new AllocationId(x));
        builder.Property(x => x.RunId).HasConversion(x => x.Value, x => new SolveRunId(x));
        builder.Property(x => x.Index);
        builder.Property(x => x.Objective).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.CreatedAt);

        builder.HasOne<SolveRun>()
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .HasConstraintName("fk_allocations_solve_runs_run_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.AllocationId)
            .HasConstraintName("fk_allocation_items_allocations_allocation_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RunId, x.Index });
    }
}