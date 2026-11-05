using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class PinRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Pin, PinId>(dbContext), IPinRepository, IPinQueries
{
    public Task<Option<Pin>> GetByParticipant(ExamSessionId sessionId, ParticipantId participantId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Pin>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Pin>> GetByRoom(RoomId roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}