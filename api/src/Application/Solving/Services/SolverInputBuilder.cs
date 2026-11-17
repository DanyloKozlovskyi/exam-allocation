using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Settings;
using Application.Solving.Mapping;
using Domain.Allocations;
using Domain.Participants;
using Domain.Rooms;
using Domain.Sessions;
using Domain.SolveRuns;
using Domain.ValueObjects;
using Solver.Model;

namespace Application.Solving.Services;

/// <summary>Domain snapshot → <see cref="SolverInput"/>, and <see cref="SolverOutput"/> → <see cref="Allocation"/> entities.</summary>
public class SolverInputBuilder(
    IRoomQueries roomQueries,
    IParticipantQueries participantQueries,
    IRuleQueries ruleQueries,
    IPinQueries pinQueries,
    ApplicationSettings settings,
    IDateTimeProvider dateTimeProvider) : ISolverInputBuilder
{
    public static SolverParticipant ToSolverParticipant(Participant participant)
    {
        var attributes = new Dictionary<string, string>(participant.Attributes, StringComparer.Ordinal);
        if (participant.Group is { } group)
        {
            attributes[AttributeKeys.Group] = group;
        }

        if (participant.School is { } school)
        {
            attributes[AttributeKeys.School] = school;
        }

        return new SolverParticipant(participant.Id.Value, attributes, participant.NeedsAccessibleSeat, participant.Name.SortKey);
    }

    public async Task<SolverInput> Build(ExamSessionId sessionId, SolveRunOptions options, CancellationToken cancellationToken)
    {
        var rooms = await roomQueries.GetActiveBySession(sessionId, cancellationToken);
        var participants = await participantQueries.GetAllBySession(sessionId, cancellationToken);
        var rules = await ruleQueries.GetAll(sessionId, cancellationToken);
        var pins = await pinQueries.GetAll(sessionId, cancellationToken);

        var solverRooms = rooms
            .Select(room => new SolverRoom(
                room.Id.Value,
                room.CenterId.Value,
                room.CostOverride ?? room.Center?.CostPerRoom ?? Domain.Centers.TestCenterConstraints.DefaultCostPerRoom,
                SolverModelMapper.ToSolverLayout(room.Layout)))
            .ToList();

        var solverParticipants = participants.Select(ToSolverParticipant).ToList();

        var solverRules = rules
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id.Value)
            .Select(SolverModelMapper.ToSolverRule)
            .ToList();

        var solverPins = pins
            .Select(pin => new SolverPin(pin.ParticipantId.Value, pin.RoomId.Value, pin.Seat?.Key.Value))
            .ToList();

        return new SolverInput(solverRooms, solverParticipants, solverRules, solverPins, ToSolveOptions(options));
    }

    public async Task<IReadOnlyList<Allocation>> ToAllocations(SolveRunId runId, ExamSessionId sessionId, SolverOutput output, CancellationToken cancellationToken)
    {
        var rooms = await roomQueries.GetActiveBySession(sessionId, cancellationToken);
        var seatIds = rooms
            .SelectMany(room => room.Seats.Select(seat => (room.Id.Value, seat.Key.Value, seat.Id)))
            .ToDictionary(x => (x.Item1, x.Item2), x => x.Id);

        var now = dateTimeProvider.UtcNow;
        return output.Allocations
            .Select(solved => Allocation.New(
                AllocationId.New(),
                runId,
                solved.Index,
                SolverModelMapper.ToDomainObjective(solved.Objective),
                solved.Items
                    .Select(item => (new ParticipantId(item.ParticipantId), new RoomId(item.RoomId), ResolveSeat(seatIds, item)))
                    .ToList(),
                now))
            .ToList();
    }

    private static SeatId ResolveSeat(IReadOnlyDictionary<(Guid RoomId, string SeatKey), SeatId> seatIds, SeatAssignment item)
    {
        if (!seatIds.TryGetValue((item.RoomId, item.SeatKey), out var seatId))
        {
            throw new InvalidOperationException($"Solver returned unknown seat {item.SeatKey} in room {item.RoomId}");
        }

        return seatId;
    }

    private SolveOptions ToSolveOptions(SolveRunOptions options) => new()
    {
        Alternatives = options.Alternatives,
        DiversityRatio = options.DiversityRatio,
        CostSlack = options.CostSlack,
        Stage1TimeLimitSeconds = options.TimeLimitSeconds,
        Stage2TimeLimitSecondsPerRoom = settings.Solver.Stage2TimeLimitSecondsPerRoom,
        MaxFeedbackIterations = settings.Solver.MaxFeedbackIterations,
        NumWorkers = settings.Solver.NumWorkers,
        RandomSeed = options.Seed,
        ObjectiveMode = EnumMapper.Map<Domain.SolveRuns.ObjectiveMode, Solver.Model.ObjectiveMode>(options.ObjectiveMode),
    };
}