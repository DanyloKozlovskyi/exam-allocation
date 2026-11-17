using Domain.Centers;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IRoomQueries
{
    Task<IReadOnlyList<Room>> GetAll(TestCenterId centerId, CancellationToken cancellationToken = default);

    Task<Option<Room>> GetById(TestCenterId centerId, RoomId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> GetActiveBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>All rooms of the session (active or not) with seats and center; used to read stored allocations.</summary>
    Task<IReadOnlyList<Room>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>Rooms with the given ids, without seats (names and centers only).</summary>
    Task<IReadOnlyList<Room>> GetByIds(IReadOnlyCollection<RoomId> ids, CancellationToken cancellationToken = default);
}