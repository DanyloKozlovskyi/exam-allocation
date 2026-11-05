using Api.Dtos;
using Application.Common.Models;
using Application.Participants.Models;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IParticipantsControllerService
{
    Task<PagedDto<ParticipantDto>> GetPage(Guid sessionId, ParticipantFilter filter, PageRequest page, CancellationToken cancellationToken);

    Task<Option<ParticipantDto>> Get(Guid sessionId, Guid participantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttributeInfoDto>> Attributes(Guid sessionId, CancellationToken cancellationToken);
}