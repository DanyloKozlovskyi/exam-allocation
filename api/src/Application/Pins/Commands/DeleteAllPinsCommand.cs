using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Pins.Exceptions;
using Domain.Pins;
using Domain.Sessions;
using LanguageExt;
using MediatR;

namespace Application.Pins.Commands;

/// <summary>Removes every pin of a session; returns how many were removed.</summary>
public record DeleteAllPinsCommand : IRequest<Either<PinException, int>>
{
    public Guid SessionId { get; init; }
}

public class DeleteAllPinsCommandHandler(IExamSessionQueries sessionQueries, IPinRepository pinRepository)
    : IRequestHandler<DeleteAllPinsCommand, Either<PinException, int>>
{
    public async Task<Either<PinException, int>> Handle(DeleteAllPinsCommand request, CancellationToken cancellationToken)
    {
        var sessionId = new ExamSessionId(request.SessionId);
        try
        {
            var session = await sessionQueries.GetById(sessionId, cancellationToken);
            if (session.IsNone)
            {
                return new PinSessionNotFoundException(PinId.Empty(), request.SessionId);
            }

            var pins = await pinRepository.GetAllTracked(sessionId, cancellationToken);
            await pinRepository.SaveBatchAsync([], pins, cancellationToken);
            return pins.Count;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new PinUnknownException(PinId.Empty(), exception);
        }
    }
}