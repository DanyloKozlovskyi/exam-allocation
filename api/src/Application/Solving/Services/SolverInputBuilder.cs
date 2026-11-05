using Domain.Allocations;
using Domain.Sessions;
using Domain.SolveRuns;
using Solver.Model;

namespace Application.Solving.Services;

public class SolverInputBuilder : ISolverInputBuilder
{
    public Task<SolverInput> Build(ExamSessionId sessionId, SolveRunOptions options, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Allocation>> ToAllocations(SolveRunId runId, ExamSessionId sessionId, SolverOutput output, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}