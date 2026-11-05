using Api.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IRoomsControllerService
{
    Task<IReadOnlyList<RoomDto>> GetAll(Guid centerId, CancellationToken cancellationToken);

    Task<Option<RoomDto>> Get(Guid centerId, Guid roomId, CancellationToken cancellationToken);

    RoomLayoutPreviewDto PreviewLayout(RoomLayoutDto layout);
}