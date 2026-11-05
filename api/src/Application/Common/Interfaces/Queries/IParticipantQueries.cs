using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IParticipantQueries
{
    Task<PagedResult<Participant>> GetPage(ExamSessionId sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken = default);

    Task<Option<Participant>> GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Participant>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttributeSummary>> GetAttributeSummary(ExamSessionId sessionId, CancellationToken cancellationToken = default);
}