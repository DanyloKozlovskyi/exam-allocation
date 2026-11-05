using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class RoomRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Room, RoomId>(dbContext), IRoomRepository, IRoomQueries
{
    public Task<Option<Room>> GetById(TestCenterId centerId, RoomId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<Room>> FindByName(TestCenterId centerId, string name, Option<RoomId> excludeId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Room>> GetAll(TestCenterId centerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Room>> GetActiveBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}