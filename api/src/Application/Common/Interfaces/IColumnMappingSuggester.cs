using Application.Imports.Models;

namespace Application.Common.Interfaces;

public interface IColumnMappingSuggester
{
    ColumnMapping Suggest(IReadOnlyList<string> headers);
}