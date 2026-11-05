using Domain.Participants;
using Domain.Pins;
using Domain.Rooms;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IPinRepository
{
    Task<Option<Pin>> GetByParticipant(ExamSessionId sessionId, ParticipantId participantId, CancellationToken cancellationToken = default);

    Task<Pin> AddAsync(Pin entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<Pin> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<Pin> UpdateAsync(Pin entity, CancellationToken cancellationToken = default);

    Task<Pin> RemoveAsync(Pin entity, CancellationToken cancellationToken = default);
}