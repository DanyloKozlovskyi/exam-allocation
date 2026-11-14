using FluentValidation;

namespace Application.Participants.Commands;

public class CreateParticipantCommandValidator : AbstractValidator<CreateParticipantCommand>
{
    public CreateParticipantCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
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