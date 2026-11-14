using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Models;
using Application.Participants.Models;
using Application.Sessions.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Api.Tests.Integration.Imports;

/// <summary>In-memory participant store standing in for the WS-C repository on this branch.</summary>
public sealed class FakeParticipantStore : IParticipantRepository, IParticipantQueries
{
    private readonly List<Participant> participants = [];
    private readonly object sync = new();

    public IReadOnlyList<Participant> All
    {
        get
        {
            lock (sync)
            {
                return participants.ToList();
            }
        }
    }

    public void Seed(params Participant[] items)
    {
        lock (sync)
        {
            participants.AddRange(items);
        }
    }

    public Task<IReadOnlyList<Participant>> GetAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Participant>>(All.Where(x => x.SessionId == sessionId).ToList());

    public Task<int> RemoveAllBySession(ExamSessionId sessionId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(participants.RemoveAll(x => x.SessionId == sessionId));
        }
    }

    public Task AddRangeAsync(IReadOnlyList<Participant> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
    {
        Seed([.. entities]);
        return Task.CompletedTask;
    }

    public Task<Participant> UpdateAsync(Participant entity, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            participants.RemoveAll(x => x.Id == entity.Id);
            participants.Add(entity);
        }

        return Task.FromResult(entity);
    }

    public Task<Participant> AddAsync(Participant entity, CancellationToken cancellationToken = default)
    {
        Seed(entity);
        return Task.FromResult(entity);
    }

    public Task<Option<Participant>> GetById(ExamSessionId sessionId, ParticipantId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Prelude.Optional(All.FirstOrDefault(x => x.SessionId == sessionId && x.Id == id)));

    public Task<Option<Participant>> FindByExternalId(ExamSessionId sessionId, string externalId, Option<ParticipantId> excludeId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Participant> RemoveAsync(Participant entity, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<PagedResult<Participant>> GetPage(ExamSessionId sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<AttributeSummary>> GetAttributeSummary(ExamSessionId sessionId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Knows exactly one session.</summary>
public sealed class FakeExamSessionQueries(ExamSession session) : IExamSessionQueries
{
    public Task<Option<ExamSession>> GetById(ExamSessionId id, CancellationToken cancellationToken = default)
        => Task.FromResult(id == session.Id ? Option<ExamSession>.Some(session) : Option<ExamSession>.None);

    public Task<IReadOnlyList<ExamSession>> GetAll(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<Option<SessionTotals>> GetTotals(ExamSessionId id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}