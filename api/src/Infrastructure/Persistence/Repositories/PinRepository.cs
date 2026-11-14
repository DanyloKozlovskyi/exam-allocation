using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class PinRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Pin, PinId>(dbContext), IPinRepository, IPinQueries
{
    public async Task<Option<Pin>> GetByParticipant(ExamSessionId sessionId, ParticipantId participantId, CancellationToken cancellationToken)
    {
        var entity = await Context.Pins
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.ParticipantId == participantId, cancellationToken);

        return entity ?? Option<Pin>.None;
    }

    public async Task<IReadOnlyList<Pin>> GetAll(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        return await Context.Pins
            .AsNoTracking()
            .Include(x => x.Seat)
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.ParticipantId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pin>> GetByRoom(RoomId roomId, CancellationToken cancellationToken)
    {
        return await Context.Pins
            .AsNoTracking()
            .Include(x => x.Seat)
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.ParticipantId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}