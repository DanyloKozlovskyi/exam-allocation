using Domain.Sessions;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SolveRunsConfiguration : IEntityTypeConfiguration<SolveRun>
{
    public const string ActiveRunIndexName = "ix_solve_runs_session_id_active";

    private const string StatusColumn = "status";

    private static readonly string ActiveRunFilter =
        $"{StatusColumn} IN ('{nameof(SolveRunStatus.Queued)}', '{nameof(SolveRunStatus.Running)}')";

    public void Configure(EntityTypeBuilder<SolveRun> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SolveRunId(x));
        builder.Property(x => x.SessionId).HasConversion(x => x.Value, x => new ExamSessionId(x));
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(ColumnTypes.EnumMaxLength);
        builder.Property(x => x.Options).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.InputHash).HasMaxLength(SolveRunConstraints.InputHashLength).IsRequired();
        builder.Property(x => x.Diagnostics).HasColumnType(ColumnTypes.Jsonb).IsRequired();
        builder.Property(x => x.Stats).HasColumnType(ColumnTypes.Jsonb);
        builder.Property(x => x.ErrorMessage).HasMaxLength(SolveRunConstraints.ErrorMessageMaxLength);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.FinishedAt);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Ignore(x => x.IsActive);

        builder.HasOne<ExamSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .HasConstraintName("fk_solve_runs_exam_sessions_session_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });

        // At most one Queued/Running run per session, enforced by the database (concurrent creates).
        builder.HasIndex(x => x.SessionId)
            .IsUnique()
            .HasFilter(ActiveRunFilter)
            .HasDatabaseName(ActiveRunIndexName);
    }
}