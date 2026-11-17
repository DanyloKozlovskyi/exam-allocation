using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Centers;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RoomRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Room, RoomId>(dbContext), IRoomRepository, IRoomQueries
{
    /// <summary>Tracked lookup including seats and center, within a center.</summary>
    public async Task<Option<Room>> GetById(TestCenterId centerId, RoomId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Rooms
            .Include(x => x.Seats)
            .Include(x => x.Center)
            .FirstOrDefaultAsync(x => x.CenterId == centerId && x.Id == id, cancellationToken);

        return entity ?? Option<Room>.None;
    }

    /// <summary>Tracked lookup including seats and center, by id only.</summary>
    public async Task<Option<Room>> GetById(RoomId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Rooms
            .Include(x => x.Seats)
            .Include(x => x.Center)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Room>.None;
    }

    public async Task<Option<Room>> FindByName(TestCenterId centerId, string name, Option<RoomId> excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var query = Context.Rooms.AsNoTracking().Where(x => x.CenterId == centerId && x.Name == normalized);
        query = excludeId.Match(id => query.Where(x => x.Id != id), () => query);
        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        return entity ?? Option<Room>.None;
    }

    public async Task<IReadOnlyList<Room>> GetAll(TestCenterId centerId, CancellationToken cancellationToken)
    {
        return await Context.Rooms
            .AsNoTracking()
            .Where(x => x.CenterId == centerId)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes the room in a single statement so that the database cascades seats and pins together
    /// (deleting tracked seats first would violate the pin -> seat foreign key).
    /// </summary>
    async Task<Room> IRoomRepository.RemoveAsync(Room entity, CancellationToken cancellationToken)
    {
        try
        {
            await Context.Rooms.Where(x => x.Id == entity.Id).ExecuteDeleteAsync(cancellationToken);
            Context.ChangeTracker.Clear();
            return entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected failure while trying to delete room '{entity.Id}'", ex);
        }
    }

    async Task<Option<Room>> IRoomQueries.GetById(TestCenterId centerId, RoomId id, CancellationToken cancellationToken)
    {
        var entity = await Context.Rooms
            .AsNoTracking()
            .Include(x => x.Seats)
            .FirstOrDefaultAsync(x => x.CenterId == centerId && x.Id == id, cancellationToken);

        return entity ?? Option<Room>.None;
    }

    public async Task<IReadOnlyList<Room>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.Rooms
            .AsNoTracking()
            .Include(x => x.Seats)
            .Include(x => x.Center)
            .Where(x => x.Center!.SessionId == sessionId)
            .OrderBy(x => x.CenterId)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetActiveBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.Rooms
            .AsNoTracking()
            .Include(x => x.Seats)
            .Include(x => x.Center)
            .Where(x => x.IsActive && x.Center!.IsActive && x.Center.SessionId == sessionId)
            .OrderBy(x => x.CenterId)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }
}