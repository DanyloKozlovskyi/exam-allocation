using Application.Common.Interfaces;
using Application.Imports.Models;

namespace Infrastructure.ImportExport.Mapping;

public class ColumnMappingSuggester : IColumnMappingSuggester
{
    public ColumnMapping Suggest(IReadOnlyList<string> headers)
    {
        throw new NotImplementedException();
    }
}