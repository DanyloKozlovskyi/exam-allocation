using Application.Allocations.Exceptions;
using Application.Allocations.Models;
using Application.Allocations.Services;
using Application.Common.Interfaces.Repositories;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using LanguageExt;
using MediatR;

namespace Application.Allocations.Commands;

public record PinAllCommand : IRequest<Either<AllocationException, IReadOnlyList<PinnedSeat>>>
{
    public Guid AllocationId { get; init; }

    public IReadOnlyList<Guid>? RoomIds { get; init; }
}

/// <summary>Pins participants of an allocation (all, or only the given rooms) to their current room and seat.</summary>
public class PinAllCommandHandler(IAllocationEvaluator evaluator, IPinRepository pinRepository)
    : IRequestHandler<PinAllCommand, Either<AllocationException, IReadOnlyList<PinnedSeat>>>
{
    public async Task<Either<AllocationException, IReadOnlyList<PinnedSeat>>> Handle(
        PinAllCommand request,
        CancellationToken cancellationToken)
    {
        var allocationId = new Domain.Allocations.AllocationId(request.AllocationId);
        var context = await evaluator.Load(allocationId, cancellationToken);
        if (context.IsNone)
        {
            return new AllocationNotFoundException(allocationId);
        }

        var loaded = context.Match(x => x, () => throw new InvalidOperationException());
        var roomFilter = request.RoomIds is { Count: > 0 } ids ? ids.ToHashSet() : null;
        var items = loaded.Allocation.Items.Where(x => roomFilter is null || roomFilter.Contains(x.RoomId.Value)).ToList();
        try
        {
            var pins = await pinRepository.GetAllTracked(loaded.Run.SessionId, cancellationToken);
            var byParticipant = pins.ToDictionary(x => x.ParticipantId);
            var batch = items.Select(x => x.ParticipantId).ToHashSet();

            // One pin per seat (as in UpsertPin): a pin of someone outside the batch on a seat this allocation
            // gives to a batch participant is dropped; pins of batch participants are moved below.
            var targetSeats = items.Select(x => x.SeatId).ToHashSet();
            var removed = pins.Where(x => !batch.Contains(x.ParticipantId) && x.SeatId is { } seat && targetSeats.Contains(seat)).ToList();

            var added = new List<Pin>();
            var result = new List<PinnedSeat>();
            foreach (var item in items)
            {
                if (byParticipant.TryGetValue(item.ParticipantId, out var pin))
                {
                    pin.MoveTo(item.RoomId, item.SeatId);
                }
                else
                {
                    pin = Pin.New(PinId.New(), loaded.Run.SessionId, item.ParticipantId, item.RoomId, item.SeatId);
                    added.Add(pin);
                }

                result.Add(new PinnedSeat(pin, loaded.SeatsById[item.SeatId].Seat.Key.Value));
            }

            await pinRepository.SaveBatchAsync(added, removed, cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            return new AllocationUnknownException(allocationId, exception);
        }
    }
}