using Application.Allocations.Exceptions;
using Domain.Pins;
using LanguageExt;
using MediatR;

namespace Application.Allocations.Commands;

public record PinAllCommand : IRequest<Either<AllocationException, IReadOnlyList<Pin>>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<Guid>? RoomIds { get; init; }
}

public class PinAllCommandHandler
    : IRequestHandler<PinAllCommand, Either<AllocationException, IReadOnlyList<Pin>>>
{
    public Task<Either<AllocationException, IReadOnlyList<Pin>>> Handle(
        PinAllCommand request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}