using FluentValidation;

namespace Application.Participants.Commands;

public class CreateParticipantCommandValidator : AbstractValidator<CreateParticipantCommand>
{
    public CreateParticipantCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
    }
}