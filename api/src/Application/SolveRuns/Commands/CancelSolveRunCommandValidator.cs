using FluentValidation;

namespace Application.SolveRuns.Commands;

public class CancelSolveRunCommandValidator : AbstractValidator<CancelSolveRunCommand>
{
    public CancelSolveRunCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
    }
}