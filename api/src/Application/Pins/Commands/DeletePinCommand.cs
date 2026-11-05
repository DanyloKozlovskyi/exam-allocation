using Application.Pins.Exceptions;
using Domain.Pins;
using LanguageExt;
using MediatR;

namespace Application.Pins.Commands;

public record DeletePinCommand : IRequest<Either<PinException, Pin>>
{
    public Guid SessionId { get; init; }

    public Guid ParticipantId { get; init; }
}

public class DeletePinCommandHandler
    : IRequestHandler<DeletePinCommand, Either<PinException, Pin>>
{
    public Task<Either<PinException, Pin>> Handle(
        DeletePinCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}