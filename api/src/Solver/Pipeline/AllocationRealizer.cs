using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Solver.Allocation;
using Solver.Expansion;
using Solver.Model;
using Solver.Precheck;
using Solver.Seating;

namespace Solver.Pipeline;

/// <summary>Outcome of stage 2 over all used rooms.</summary>
internal sealed record Realization(
    bool Cancelled,
    IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> SeatingByRoom,
    IReadOnlyList<Guid> FailedRooms,
    bool AllOptimal,
    long Penalty,
    IReadOnlyDictionary<Guid, long> PenaltyByRule,
    long ElapsedMs);

/// <summary>Turns stage-1 counts into seats (04 §5) and participants (04 §9); shared by the pipeline and alternatives.</summary>
internal sealed class AllocationRealizer(ISeatingSolver seatingSolver, IExpander expander)
{
    /// <summary>Feedback cuts of 04 §6 for the failed rooms; rooms without a possible cut are returned separately.</summary>
    public static (IReadOnlyList<Stage1Cut> Cuts, IReadOnlyList<Diagnostic> Unresolved) BuildCuts(
        SolverInput input,
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyList<RoomClassCount> counts,
        IReadOnlyList<Guid> failedRooms)
    {
        var participants = input.Participants.ToDictionary(p => p.ParticipantId);
        var hardRules = input.Rules
            .Where(r => r.IsHard)
            .Select(r => r.Parameters)
            .OfType<NoAdjacentByAttributeParameters>()
            .Select(p => p.AttributeKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var cuts = new List<Stage1Cut>();
        var unresolved = new List<Diagnostic>();
        foreach (var roomId in failedRooms)
        {
            var best = hardRules
                .SelectMany(key => ValueCounts(input, participants, classes, counts, roomId, key).Select(v => (Key: key, v.Value, v.Count)))
                .OrderByDescending(v => v.Count)
                .ThenBy(v => v.Key, StringComparer.Ordinal)
                .ThenBy(v => v.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (best.Key is null || best.Count <= 1)
            {
                unresolved.Add(DiagnosticFactory.Error(DiagnosticCode.SeatingInfeasible, null, (DiagnosticArgument.RoomId, roomId)));
                continue;
            }

            cuts.Add(new Stage1Cut(roomId, best.Key, best.Value, best.Count - 1));
        }

        return (cuts, unresolved);
    }

    public static Diagnostic SeatingInfeasible(Stage1Cut cut)
        => DiagnosticFactory.Error(
            DiagnosticCode.SeatingInfeasible,
            null,
            (DiagnosticArgument.RoomId, cut.RoomId),
            (DiagnosticArgument.AttributeKey, cut.AttributeKey),
            (DiagnosticArgument.Value, cut.Value),
            (DiagnosticArgument.Size, cut.MaxCount + 1));

    public static ObjectiveBreakdown Objective(SolverInput input, ObjectiveBreakdown? stage1, Realization realization, IReadOnlyList<SeatAssignment> items)
    {
        var roomsById = input.Rooms.ToDictionary(r => r.RoomId);
        var used = items.Select(i => i.RoomId).Distinct().Where(roomsById.ContainsKey).ToList();
        var penaltyByRule = new Dictionary<Guid, long>(stage1?.PenaltyByRule ?? new Dictionary<Guid, long>());
        foreach (var (ruleId, penalty) in realization.PenaltyByRule)
        {
            penaltyByRule[ruleId] = penaltyByRule.GetValueOrDefault(ruleId) + penalty;
        }

        return new ObjectiveBreakdown(used.Count, used.Sum(r => roomsById[r].Cost), (stage1?.SoftPenalty ?? 0) + realization.Penalty, penaltyByRule);
    }

    /// <summary>Rebuilds class-level seatings per room from an expanded allocation (for seat-level diversity).</summary>
    public static IReadOnlyDictionary<Guid, IReadOnlyList<ClassSeatAssignment>> ToClassSeatings(
        SolverInput input,
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyList<SeatAssignment> items)
    {
        var classByParticipant = classes.SelectMany(c => c.ParticipantIds.Select(p => (p, c.Index))).ToDictionary(e => e.p, e => e.Index);
        return items
            .GroupBy(i => i.RoomId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ClassSeatAssignment>)g
                    .Select(i => classByParticipant.TryGetValue(i.ParticipantId, out var index)
                        ? new ClassSeatAssignment(index, null, i.SeatKey)
                        : new ClassSeatAssignment(null, i.ParticipantId, i.SeatKey))
                    .OrderBy(a => a.SeatKey, StringComparer.Ordinal)
                    .ToList());
    }

    /// <summary>Hash of sorted (participantId, roomId, seatKey) for deduplication (04 §8).</summary>
    public static string Hash(IReadOnlyList<SeatAssignment> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items.OrderBy(i => i.ParticipantId))
        {
            builder.Append(item.ParticipantId.ToString("N")).Append(item.RoomId.ToString("N")).Append(item.SeatKey).Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    public async Task<Realization> SeatAsync(
        SolverInput input,
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyList<RoomClassCount> counts,
        int randomSeed,
        IReadOnlyDictionary<Guid, IReadOnlyList<IReadOnlyList<ClassSeatAssignment>>>? previousSeatings,
        IProgress<SolveProgress> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var participants = input.Participants.ToDictionary(p => p.ParticipantId);
        var pins = input.Pins.Where(p => participants.ContainsKey(p.ParticipantId)).DistinctBy(p => p.ParticipantId).ToList();
        var countsByRoom = counts.GroupBy(c => c.RoomId).ToDictionary(g => g.Key, g => (IReadOnlyList<RoomClassCount>)g.ToList());
        var pinsByRoom = pins.GroupBy(p => p.RoomId).ToDictionary(g => g.Key, g => g.ToList());
        var rooms = input.Rooms.Where(r => countsByRoom.ContainsKey(r.RoomId) || pinsByRoom.ContainsKey(r.RoomId)).ToList();
        var results = new ConcurrentDictionary<Guid, SeatingResult>();
        var done = 0;
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken };
        progress.Report(new SolveProgress(SolveStage.Stage2, 0, null, null, 0, rooms.Count, null));
        try
        {
            await Parallel.ForEachAsync(rooms, parallel, (room, token) =>
            {
                var roomPins = pinsByRoom.GetValueOrDefault(room.RoomId) ?? [];
                var request = new SeatingRequest(
                    room,
                    classes,
                    countsByRoom.GetValueOrDefault(room.RoomId) ?? [],
                    input.Rules,
                    roomPins,
                    roomPins.Select(p => participants[p.ParticipantId]).ToList(),
                    randomSeed,
                    input.Options.Stage2TimeLimitSecondsPerRoom,
                    previousSeatings?.GetValueOrDefault(room.RoomId) ?? []);
                results[room.RoomId] = seatingSolver.Solve(request, token);
                var finished = Interlocked.Increment(ref done);
                progress.Report(new SolveProgress(SolveStage.Stage2, Percent(finished, rooms.Count), null, null, finished, rooms.Count, null));
                return ValueTask.CompletedTask;
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new Realization(true, new Dictionary<Guid, IReadOnlyList<ClassSeatAssignment>>(), [], false, 0, new Dictionary<Guid, long>(), stopwatch.ElapsedMilliseconds);
        }

        var cancelled = cancellationToken.IsCancellationRequested || results.Values.Any(r => r.Status == SolveStatus.Cancelled);
        var succeeded = results.Where(r => r.Value.Status is SolveStatus.Optimal or SolveStatus.Feasible).ToList();
        var succeededIds = succeeded.Select(s => s.Key).ToHashSet();
        var failed = rooms.Select(r => r.RoomId).Where(id => !succeededIds.Contains(id)).ToList();
        var penaltyByRule = new Dictionary<Guid, long>();
        foreach (var (ruleId, penalty) in succeeded.SelectMany(s => s.Value.PenaltyByRule))
        {
            penaltyByRule[ruleId] = penaltyByRule.GetValueOrDefault(ruleId) + penalty;
        }

        return new Realization(
            cancelled,
            succeeded.ToDictionary(s => s.Key, s => s.Value.Assignments),
            failed,
            succeeded.All(s => s.Value.Status == SolveStatus.Optimal),
            succeeded.Sum(s => s.Value.Penalty),
            penaltyByRule,
            stopwatch.ElapsedMilliseconds);
    }

    public IReadOnlyList<SeatAssignment> Expand(SolverInput input, IReadOnlyList<ParticipantClass> classes, Realization realization, int alternativeIndex)
        => expander.Expand(classes, realization.SeatingByRoom, input.Participants, alternativeIndex, input.Options.RandomSeed);

    private static double? Percent(int done, int total) => total == 0 ? null : 100.0 * done / total;

    private static IEnumerable<(string Value, int Count)> ValueCounts(
        SolverInput input,
        IReadOnlyDictionary<Guid, SolverParticipant> participants,
        IReadOnlyList<ParticipantClass> classes,
        IReadOnlyList<RoomClassCount> counts,
        Guid roomId,
        string key)
    {
        var classValues = counts
            .Where(c => c.RoomId == roomId && AttributeValues.TryGet(classes[c.ClassIndex], key, out _))
            .Select(c => (Value: classes[c.ClassIndex].Values[key], c.Count));
        var pinValues = input.Pins
            .Where(p => p.RoomId == roomId && participants.ContainsKey(p.ParticipantId))
            .DistinctBy(p => p.ParticipantId)
            .Select(p => (Has: AttributeValues.TryGet(participants[p.ParticipantId], key, out var value), Value: value))
            .Where(p => p.Has)
            .Select(p => (Value: p.Value!, Count: 1));
        return classValues.Concat(pinValues)
            .GroupBy(v => v.Value, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Sum(v => v.Count)));
    }
}