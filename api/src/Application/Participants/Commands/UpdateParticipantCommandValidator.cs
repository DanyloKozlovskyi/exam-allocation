using FluentValidation;

namespace Application.Participants.Commands;

public class UpdateParticipantCommandValidator : AbstractValidator<UpdateParticipantCommand>
{
    public UpdateParticipantCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
    }
}