using Application.Imports.Models;
using FluentAssertions;
using Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Infrastructure.Tests.ImportExport;

public class InMemoryImportCacheTests
{
    [Fact]
    public void ShouldPutGetAndRemoveImports()
    {
        // Arrange
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new InMemoryImportCache(memoryCache, ImportExportTestData.Settings());
        var import = new CachedImport(Guid.NewGuid(), Guid.NewGuid(), "people.csv", [1, 2, 3]);

        // Act
        cache.Put(import);
        var found = cache.Get(import.ImportId);
        var missing = cache.Get(Guid.NewGuid());
        cache.Remove(import.ImportId);
        var removed = cache.Get(import.ImportId);

        // Assert
        found.IsSome.Should().BeTrue();
        found.IfNoneUnsafe(() => null).Should().Be(import);
        missing.IsNone.Should().BeTrue();
        removed.IsNone.Should().BeTrue();
    }
}