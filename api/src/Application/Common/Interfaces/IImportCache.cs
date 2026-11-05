using Application.Imports.Models;
using LanguageExt;

namespace Application.Common.Interfaces;

public interface IImportCache
{
    void Put(CachedImport import);

    Option<CachedImport> Get(Guid importId);

    void Remove(Guid importId);
}