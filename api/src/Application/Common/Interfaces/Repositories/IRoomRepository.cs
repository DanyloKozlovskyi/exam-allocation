using Domain.Centers;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<Option<Room>> GetById(TestCenterId centerId, RoomId id, CancellationToken cancellationToken = default);

    Task<Option<Room>> FindByName(TestCenterId centerId, string name, Option<RoomId> excludeId, CancellationToken cancellationToken = default);

    Task<Room> AddAsync(Room entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Room> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<Room> UpdateAsync(Room entity, CancellationToken cancellationToken = default);

    Task<Room> RemoveAsync(Room entity, CancellationToken cancellationToken = default);
}