using Application.Common.Interfaces;
using Application.Imports.Models;
using LanguageExt;

namespace Infrastructure.Caching;

public class InMemoryImportCache : IImportCache
{
    public void Put(CachedImport import)
    {
        throw new NotImplementedException();
    }

    public Option<CachedImport> Get(Guid importId)
    {
        throw new NotImplementedException();
    }

    public void Remove(Guid importId)
    {
        throw new NotImplementedException();
    }
}