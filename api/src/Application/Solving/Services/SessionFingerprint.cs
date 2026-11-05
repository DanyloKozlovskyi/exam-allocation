using Domain.Sessions;

namespace Application.Solving.Services;

public class SessionFingerprint : ISessionFingerprint
{
    public Task<string> Compute(ExamSessionId sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}