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
        // Пробуем достать из кэша
        if (memoryCache.TryGetValue(enumTypeName, out ImmutableArray<string> cached))
            return Task.FromResult(cached);

        // Если нет в кэше — создаём
        var enumType = DomainAssemblyInfo.GetAssembly()
            .GetEnum(DomainAssemblyInfo.GlossariesNamespace, enumTypeName)
            ?? throw new LimsOperationException($"Справочник \"{enumTypeName}\" не найден.");

        var names = enumType.GetDisplayNames().ToImmutableArray();

        // Кладём без срока жизни
        memoryCache.Set(enumTypeName, names);

        return Task.FromResult(names);
    }

    public async Task<IReadOnlyList<string>> GetDisplayNamesAsync(string glossaryName)
    {
        var items = await GetGlossaryDisplayNamesAsync(glossaryName);
        return items;
    }
}

using Microsoft.Extensions.Caching.Memory;
using System.Collections.Immutable;

public interface IGlossaryUseCase
{
    Task<ImmutableArray<string>> GetDisplayNamesAsync(string glossaryName);
    void Invalidate(string glossaryName);
}

public sealed class GlossaryUseCase : IGlossaryUseCase
{
    private readonly IMemoryCache memoryCache;

    public GlossaryUseCase(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    /// <summary>
    /// Возвращает имена для указанного enum.
    /// Ключ кэша = имя enum без префиксов, значение — ImmutableArray<string>.
    /// Данные живут до Remove() или рестарта приложения.
    /// </summary>
    public Task<ImmutableArray<string>> GetDisplayNamesAsync(string glossaryName)
    {
        return memoryCache.GetOrCreateAsync(glossaryName, entry =>
        {
            // Не задаём AbsoluteExpiration/SlidingExpiration — бессрочный кэш
            var enumType = DomainAssemblyInfo.GetAssembly()
                .GetEnum(DomainAssemblyInfo.GlossariesNamespace, glossaryName)
                ?? throw new LimsOperationException($"Справочник \"{glossaryName}\" не найден.");

            // GetDisplayNames гарантированно не возвращает null
            var names = enumType.GetDisplayNames().ToImmutableArray();

            return Task.FromResult(names);
        });
    }

    /// <summary>
    /// Удаляет элемент из кэша, чтобы при следующем запросе пересоздать.
    /// </summary>
    public void Invalidate(string glossaryName) => memoryCache.Remove(glossaryName);
}
