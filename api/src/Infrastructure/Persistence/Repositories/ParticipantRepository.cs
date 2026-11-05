using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Infrastructure.Persistence.Repositories;

public class ParticipantRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Participant, ParticipantId>(dbContext), IParticipantRepository, IParticipantQueries
{
    public Task<Option<Participant>> GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<Participant>> FindByExternalId(ExamSessionId sessionId, string externalId, Option<ParticipantId> excludeId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> RemoveAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResult<Participant>> GetPage(ExamSessionId sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Participant>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AttributeSummary>> GetAttributeSummary(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}