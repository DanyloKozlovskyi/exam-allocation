using FluentValidation;

namespace Application.SolveRuns.Commands;

public class StartSolveRunCommandValidator : AbstractValidator<StartSolveRunCommand>
{
    public StartSolveRunCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Options).NotEmpty();
    }
}