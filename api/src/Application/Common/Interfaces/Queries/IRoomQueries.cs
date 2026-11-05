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
}