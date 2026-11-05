using FluentValidation;

namespace Application.Participants.Commands;

public class DeleteParticipantCommandValidator : AbstractValidator<DeleteParticipantCommand>
{
    public DeleteParticipantCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}