using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Models;
using Application.Participants.Models;
using Domain.Participants;
using Domain.Sessions;
using LanguageExt;

namespace Api.Services.Implementation;

public class ParticipantsControllerService(IParticipantQueries participantQueries) : IParticipantsControllerService
{
    public async Task<PagedDto<ParticipantDto>> GetPage(Guid sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        var result = await participantQueries.GetPage(new ExamSessionId(sessionId), filter, page, cancellationToken);
        return PagedDto<ParticipantDto>.From(result, ParticipantDto.FromDomain);
    }

    public async Task<Option<ParticipantDto>> Get(Guid sessionId, Guid participantId, CancellationToken cancellationToken)
    {
        var participant = await participantQueries.GetById(new ExamSessionId(sessionId), new ParticipantId(participantId), cancellationToken);
        return participant.Map(ParticipantDto.FromDomain);
    }

    public async Task<IReadOnlyList<AttributeInfoDto>> Attributes(Guid sessionId, CancellationToken cancellationToken)
    {
        var summary = await participantQueries.GetAttributeSummary(new ExamSessionId(sessionId), cancellationToken);
        return summary.Select(AttributeInfoDto.FromModel).ToList();
    }
}