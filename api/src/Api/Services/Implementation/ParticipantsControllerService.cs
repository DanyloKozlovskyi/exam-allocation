using Api.Dtos;
using Api.Services.Abstract;
using Application.Common.Models;
using Application.Participants.Models;
using LanguageExt;

namespace Api.Services.Implementation;

public class ParticipantsControllerService : IParticipantsControllerService
{
    public Task<PagedDto<ParticipantDto>> GetPage(Guid sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<ParticipantDto>> Get(Guid sessionId, Guid participantId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AttributeInfoDto>> Attributes(Guid sessionId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}