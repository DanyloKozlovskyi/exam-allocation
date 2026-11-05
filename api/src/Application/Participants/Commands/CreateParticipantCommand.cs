using Application.Participants.Exceptions;
using Domain.Participants;
using LanguageExt;
using MediatR;

namespace Application.Participants.Commands;

public record CreateParticipantCommand : IRequest<Either<ParticipantException, Participant>>
{
    public Guid SessionId { get; init; }

    public string? ExternalId { get; init; }

    public string? LastName { get; init; }

    public string? FirstName { get; init; }

    public string? MiddleName { get; init; }

    public string? Group { get; init; }

    public string? School { get; init; }

    public bool NeedsAccessibleSeat { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    public string? Notes { get; init; }
}

public class CreateParticipantCommandHandler
    : IRequestHandler<CreateParticipantCommand, Either<ParticipantException, Participant>>
{
    public Task<Either<ParticipantException, Participant>> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}