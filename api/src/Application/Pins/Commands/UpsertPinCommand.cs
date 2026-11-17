using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Pins.Exceptions;
using Domain.Centers;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using Domain.ValueObjects;
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

public class UpsertPinCommandHandler(
    IExamSessionRepository sessionRepository,
    IParticipantRepository participantRepository,
    IRoomRepository roomRepository,
    IPinRepository pinRepository,
    IPinQueries pinQueries)
    : IRequestHandler<UpsertPinCommand, Either<PinException, Pin>>
{
    public Task<Either<PinException, Pin>> Handle(
        UpsertPinCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        var participantId = new ParticipantId(request.ParticipantId);
        var roomId = new RoomId(request.RoomId);

        return EnsureSessionExists(sessionId, cancellationToken)
            .BindAsync(_ => EnsureParticipantExists(sessionId, participantId, cancellationToken))
            .BindAsync(_ => GetRoom(sessionId, roomId, cancellationToken))
            .BindAsync(room => ResolveSeat(room, request.SeatKey))
            .BindAsync(seat => EnsureSeatIsFree(sessionId, participantId, seat, cancellationToken))
            .BindAsync(seat => Upsert(sessionId, participantId, roomId, seat, cancellationToken));
    }

    private static Task<Either<PinException, Option<Seat>>> ResolveSeat(Room room, string? seatKey)
    {
        if (string.IsNullOrWhiteSpace(seatKey))
        {
            return Task.FromResult<Either<PinException, Option<Seat>>>(Option<Seat>.None);
        }

        var key = SeatKey.Parse(seatKey.Trim());
        var seat = room.Seats.FirstOrDefault(x => x.Key == key);
        return Task.FromResult<Either<PinException, Option<Seat>>>(seat is null
            ? new PinSeatNotFoundException(PinId.Empty(), seatKey)
            : Option<Seat>.Some(seat));
    }

    private async Task<Either<PinException, ExamSession>> EnsureSessionExists(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetById(sessionId, cancellationToken);
        return session.Match<Either<PinException, ExamSession>>(
            s => s,
            () => new PinSessionNotFoundException(PinId.Empty(), sessionId.Value));
    }

    private async Task<Either<PinException, Participant>> EnsureParticipantExists(
        ExamSessionId sessionId,
        ParticipantId participantId,
        CancellationToken cancellationToken)
    {
        var participant = await participantRepository.GetById(sessionId, participantId, cancellationToken);
        return participant.Match<Either<PinException, Participant>>(
            p => p,
            () => new PinParticipantNotFoundException(PinId.Empty(), participantId.Value));
    }

    private async Task<Either<PinException, Room>> GetRoom(ExamSessionId sessionId, RoomId roomId, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetById(roomId, cancellationToken);
        return room
            .Filter(r => r.Center is not null && r.Center.SessionId == sessionId)
            .Match<Either<PinException, Room>>(
                r => r,
                () => new PinRoomNotFoundException(PinId.Empty(), roomId.Value));
    }

    private async Task<Either<PinException, Option<Seat>>> EnsureSeatIsFree(
        ExamSessionId sessionId,
        ParticipantId participantId,
        Option<Seat> seat,
        CancellationToken cancellationToken)
    {
        if (seat.IsNone)
        {
            return seat;
        }

        var seatId = seat.Map(x => x.Id).IfNone(SeatId.Empty());
        var pins = await pinQueries.GetAll(sessionId, cancellationToken);
        var taken = pins.FirstOrDefault(x => x.SeatId == seatId && x.ParticipantId != participantId);
        return taken is null
            ? seat
            : new PinConflictException(taken.Id, $"Seat is already pinned to participant {taken.ParticipantId}");
    }

    private async Task<Either<PinException, Pin>> Upsert(
        ExamSessionId sessionId,
        ParticipantId participantId,
        RoomId roomId,
        Option<Seat> seat,
        CancellationToken cancellationToken)
    {
        SeatId? seatId = seat.Map(x => x.Id).IfNoneUnsafe((SeatId?)null);
        var existing = await pinRepository.GetByParticipant(sessionId, participantId, cancellationToken);
        var pinId = existing.Map(x => x.Id).IfNone(PinId.New);
        try
        {
            return await existing.MatchAsync(
                async pin =>
                {
                    pin.MoveTo(roomId, seatId);
                    return await pinRepository.UpdateAsync(pin, cancellationToken);
                },
                async () => await pinRepository.AddAsync(Pin.New(pinId, sessionId, participantId, roomId, seatId), cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return new PinArgumentException(pinId, exception.Message);
        }
        catch (Exception exception)
        {
            return new PinUnknownException(pinId, exception);
        }
    }
}