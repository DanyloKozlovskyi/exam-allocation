using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IParticipantRepository
{
    Task<Option<Participant>> GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken = default);

    Task<Option<Participant>> FindByExternalId(ExamSessionId sessionId, string externalId, Option<ParticipantId> excludeId, CancellationToken cancellationToken = default);

    Task<int> RemoveAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<Participant> AddAsync(Participant entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Participant> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<Participant> UpdateAsync(Participant entity, CancellationToken cancellationToken = default);

    Task<Participant> RemoveAsync(Participant entity, CancellationToken cancellationToken = default);
}