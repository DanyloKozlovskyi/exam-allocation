using Application.Common.Interfaces;
using Application.Imports.Models;
using Domain.Participants;

namespace Infrastructure.ImportExport.Validation;

public class ParticipantRowValidator : IParticipantRowValidator
{
    public ImportValidation Validate(WorkbookSheet sheet, ImportOptions options, IReadOnlyList<Participant> existing)
    {
        throw new NotImplementedException();
    }
}