using Application.Imports.Models;

namespace Application.Common.Interfaces;

public interface IParticipantFileReader
{
    WorkbookPreview Read(Stream content, string fileName);
}