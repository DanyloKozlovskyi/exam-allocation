using Application.Sessions.Models;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IExamSessionRepository
{
    Task<Option<ExamSession>> GetById(ExamSessionId id, CancellationToken cancellationToken = default);

    Task<Option<ExamSession>> FindByName(string name, Option<ExamSessionId> excludeId, CancellationToken cancellationToken = default);

    Task<ExamSession> AddAsync(ExamSession entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyList<ExamSession> entities, bool saveChanges = true, CancellationToken cancellationToken = default);

    Task<ExamSession> UpdateAsync(ExamSession entity, CancellationToken cancellationToken = default);

    Task<ExamSession> RemoveAsync(ExamSession entity, CancellationToken cancellationToken = default);
}