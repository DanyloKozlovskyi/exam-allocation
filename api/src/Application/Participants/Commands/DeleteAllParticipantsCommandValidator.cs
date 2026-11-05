using FluentValidation;

namespace Application.Participants.Commands;

public class DeleteAllParticipantsCommandValidator : AbstractValidator<DeleteAllParticipantsCommand>
{
    public DeleteAllParticipantsCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}