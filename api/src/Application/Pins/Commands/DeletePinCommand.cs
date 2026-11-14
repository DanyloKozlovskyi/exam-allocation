using Application.Common.Interfaces.Repositories;
using Application.Pins.Exceptions;
using Domain.Participants;
using Domain.Pins;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Pins.Commands;

public record DeletePinCommand : IRequest<Either<PinException, Pin>>
{
    public Guid SessionId { get; init; }

    public Guid ParticipantId { get; init; }
}

public class DeletePinCommandHandler(IPinRepository pinRepository)
    : IRequestHandler<DeletePinCommand, Either<PinException, Pin>>
{
    public Task<Either<PinException, Pin>> Handle(
        DeletePinCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        var participantId = new ParticipantId(request.ParticipantId);

        return GetPin(sessionId, participantId, cancellationToken)
            .BindAsync(pin => DeleteEntity(pin, cancellationToken));
    }

    private async Task<Either<PinException, Pin>> GetPin(ExamSessionId sessionId, ParticipantId participantId, CancellationToken cancellationToken)
    {
        var pin = await pinRepository.GetByParticipant(sessionId, participantId, cancellationToken);
        return pin.Match<Either<PinException, Pin>>(
            p => p,
            () => new PinNotFoundException(PinId.Empty()));
    }

    private async Task<Either<PinException, Pin>> DeleteEntity(Pin pin, CancellationToken cancellationToken)
    {
        try
        {
            return await pinRepository.RemoveAsync(pin, cancellationToken);
        }
        catch (Exception exception)
        {
            return new PinUnknownException(pin.Id, exception);
        }
    }
}