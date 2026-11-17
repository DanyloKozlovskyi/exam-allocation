using Domain.SolveRuns;
using FluentValidation;

namespace Application.SolveRuns.Commands;

public class StartSolveRunCommandValidator : AbstractValidator<StartSolveRunCommand>
{
    private const double MinRatio = 0;
    private const double MaxRatio = 1;

    public StartSolveRunCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Options).NotNull();
        RuleFor(x => x.Options.Alternatives).InclusiveBetween(SolveRunOptions.MinAlternatives, SolveRunOptions.MaxAlternatives);
        RuleFor(x => x.Options.DiversityRatio).InclusiveBetween(MinRatio, MaxRatio);
        RuleFor(x => x.Options.CostSlack).GreaterThanOrEqualTo(MinRatio);
        RuleFor(x => x.Options.TimeLimitSeconds).InclusiveBetween(SolveRunOptions.MinTimeLimitSeconds, SolveRunOptions.MaxTimeLimitSeconds);
        RuleFor(x => x.Options.ObjectiveMode).IsInEnum();
    }
}