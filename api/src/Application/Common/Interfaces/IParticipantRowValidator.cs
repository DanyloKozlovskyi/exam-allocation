using Application.Imports.Models;
using Domain.Participants;

namespace Application.Common.Interfaces;

public interface IParticipantRowValidator
{
    ImportValidation Validate(WorkbookSheet sheet, ImportOptions options, IReadOnlyList<Participant> existing);
}