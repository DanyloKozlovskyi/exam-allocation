using Api.Dtos;
using Api.Services.Abstract;
using LanguageExt;

namespace Api.Services.Implementation;

public class RoomsControllerService : IRoomsControllerService
{
    public Task<IReadOnlyList<RoomDto>> GetAll(Guid centerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Option<RoomDto>> Get(Guid centerId, Guid roomId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public RoomLayoutPreviewDto PreviewLayout(RoomLayoutDto layout)
    {
        throw new NotImplementedException();
    }
}