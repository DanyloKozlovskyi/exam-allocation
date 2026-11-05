using Domain.Allocations;
using Domain.Sessions;
using Domain.SolveRuns;
using Solver.Model;

namespace Application.Solving.Services;

public interface ISolverInputBuilder
{
    Task<SolverInput> Build(ExamSessionId sessionId, SolveRunOptions options, CancellationToken cancellationToken);

    Task<IReadOnlyList<Allocation>> ToAllocations(SolveRunId runId, ExamSessionId sessionId, SolverOutput output, CancellationToken cancellationToken);
}