using FluentValidation;

namespace Application.Participants.Commands;

public class UpdateParticipantCommandValidator : AbstractValidator<UpdateParticipantCommand>
{
    public UpdateParticipantCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        this.AddParticipantRules(
            x => x.ExternalId,
            x => x.LastName,
            x => x.FirstName,
            x => x.MiddleName,
            x => x.Group,
            x => x.School,
            x => x.Attributes,
            x => x.Notes);
    }
}