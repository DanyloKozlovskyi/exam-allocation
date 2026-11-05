using Microsoft.Extensions.DependencyInjection;
using Solver.Allocation;
using Solver.Alternatives;
using Solver.Expansion;
using Solver.Layout;
using Solver.Pipeline;
using Solver.Precheck;
using Solver.Seating;
using Solver.Validation;

namespace Solver;

public static class DependencyInjection
{
    public static IServiceCollection AddSolver(this IServiceCollection services)
    {
        services.AddSingleton<ISeatGenerator, SeatGenerator>();
        services.AddSingleton<IAdjacencyBuilder, AdjacencyBuilder>();
        services.AddSingleton<IIndependentSetCalculator, IndependentSetCalculator>();
        services.AddSingleton<IClassBuilder, ClassBuilder>();
        services.AddSingleton<IStage1Solver, Stage1Solver>();
        services.AddSingleton<IConflictExplainer, ConflictExplainer>();
        services.AddSingleton<IPrecheck, Precheck.Precheck>();
        services.AddSingleton<ISeatingSolver, SeatingSolver>();
        services.AddSingleton<IExpander, Expander>();
        services.AddSingleton<IAlternativesGenerator, AlternativesGenerator>();
        services.AddSingleton<IAllocationValidator, AllocationValidator>();
        services.AddSingleton<ISolverPipeline, SolverPipeline>();
        return services;
    }
}