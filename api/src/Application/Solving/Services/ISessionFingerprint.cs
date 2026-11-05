using Domain.Sessions;

namespace Application.Solving.Services;

public interface ISessionFingerprint
{
    Task<string> Compute(ExamSessionId sessionId, CancellationToken cancellationToken);
}