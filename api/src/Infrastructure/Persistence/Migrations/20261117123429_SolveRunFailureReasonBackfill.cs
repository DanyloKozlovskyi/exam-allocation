using Application.Solving.Services;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore.Migrations;
using FailureReason = Domain.SolveRuns.SolveRunFailureReason;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SolveRunFailureReasonBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Failed runs from before `failure_reason` existed (review P3-5): derive it from the stored message.
            migrationBuilder.Sql(
                $"UPDATE solve_runs SET failure_reason = CASE "
                + $"WHEN error_message = '{ApplicationDbContextInitialiser.InterruptedRunMessage}' THEN '{nameof(FailureReason.Interrupted)}' "
                + $"WHEN error_message = '{SolveRunExecutor.NoSolutionInTimeMessage}' THEN '{nameof(FailureReason.NoSolutionInTime)}' "
                + $"ELSE '{nameof(FailureReason.SolverError)}' END "
                + $"WHERE status = '{nameof(SolveRunStatus.Failed)}' AND failure_reason IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: the column itself is dropped by SolveRunFailureReason.Down.
        }
    }
}