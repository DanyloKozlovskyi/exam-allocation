using Application.Sessions.Models;
using Domain.Sessions;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IExamSessionQueries
{
    Task<IReadOnlyList<ExamSession>> GetAll(CancellationToken cancellationToken = default);

    Task<Option<ExamSession>> GetById(ExamSessionId id, CancellationToken cancellationToken = default);

    Task<Option<SessionTotals>> GetTotals(ExamSessionId id, CancellationToken cancellationToken = default);
}