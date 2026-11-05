using Application.Pins.Exceptions;
using Domain.Pins;
using LanguageExt;
using MediatR;

namespace Application.Pins.Commands;

public record UpsertPinCommand : IRequest<Either<PinException, Pin>>
{
    public Guid SessionId { get; init; }

    public Guid ParticipantId { get; init; }

    public Guid RoomId { get; init; }

    public string? SeatKey { get; init; }
}

public class UpsertPinCommandHandler
    : IRequestHandler<UpsertPinCommand, Either<PinException, Pin>>
{
    public Task<Either<PinException, Pin>> Handle(
        UpsertPinCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}