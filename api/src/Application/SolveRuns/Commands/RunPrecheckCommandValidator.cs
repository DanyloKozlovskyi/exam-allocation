using FluentValidation;

namespace Application.SolveRuns.Commands;

public class RunPrecheckCommandValidator : AbstractValidator<RunPrecheckCommand>
{
    public RunPrecheckCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}