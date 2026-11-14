using Domain.Sessions;
using FluentValidation;

namespace Application.Sessions.Commands;

public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ExamSessionConstraints.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ExamSessionConstraints.DescriptionMaxLength);
    }
}