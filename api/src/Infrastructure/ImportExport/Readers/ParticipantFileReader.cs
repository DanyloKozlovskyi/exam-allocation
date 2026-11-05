using Application.Common.Interfaces;
using Application.Imports.Models;

namespace Infrastructure.ImportExport.Readers;

public class ParticipantFileReader : IParticipantFileReader
{
    public WorkbookPreview Read(Stream content, string fileName)
    {
        throw new NotImplementedException();
    }
}