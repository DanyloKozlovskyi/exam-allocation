using Domain.Sessions;
using FluentValidation;

namespace Application.Sessions.Commands;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ExamSessionConstraints.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ExamSessionConstraints.DescriptionMaxLength);
    }
}