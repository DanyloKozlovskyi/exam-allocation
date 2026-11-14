using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Imports.Models;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

/// <summary>Keeps uploaded import files in memory for <see cref="ImportSettings.CacheMinutes"/> (sliding).</summary>
public class InMemoryImportCache(IMemoryCache cache, ApplicationSettings settings) : IImportCache
{
    public void Put(CachedImport import)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(settings.Import.CacheMinutes),
        };
        cache.Set(CacheKeys.Import(import.ImportId), import, options);
    }

    public Option<CachedImport> Get(Guid importId)
        => cache.TryGetValue(CacheKeys.Import(importId), out CachedImport? import) && import is not null
            ? Option<CachedImport>.Some(import)
            : Option<CachedImport>.None;

    public void Remove(Guid importId)
        => cache.Remove(CacheKeys.Import(importId));
}