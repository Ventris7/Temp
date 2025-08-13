using Microsoft.Extensions.Caching.Memory;
using System.Collections.Immutable;

public class GlossaryUseCase : IGlossaryUseCase
{
    private readonly IMemoryCache memoryCache;

    public GlossaryUseCase(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    private Task<ImmutableArray<string>> GetGlossaryDisplayNamesAsync(string enumTypeName)
    {
        return MemoryCacheExtensions.GetOrCreateAsync<ImmutableArray<string>>(
            memoryCache,
            enumTypeName, // ключ — имя enum
            entry =>
            {
                // никаких expiration, элемент живёт до Remove() или рестарта
                var enumType = DomainAssemblyInfo.GetAssembly()
                    .GetEnum(DomainAssemblyInfo.GlossariesNamespace, enumTypeName)
                    ?? throw new LimsOperationException($"Справочник \"{enumTypeName}\" не найден.");

                // GetDisplayNames гарантированно не возвращает null
                var names = enumType.GetDisplayNames().ToImmutableArray();
                return Task.FromResult(names);
            });
    }

    public async Task<IReadOnlyList<string>> GetDisplayNamesAsync(string glossaryName)
    {
        var items = await GetGlossaryDisplayNamesAsync(glossaryName);
        return items;
    }
}
