using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IPinQueries
{
    Task<IReadOnlyList<Pin>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pin>> GetByRoom(RoomId roomId, CancellationToken cancellationToken = default);
}