using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

public static class ModelBuilderExtensions
{
    public static void ApplyAllConfigurationsFromServices(this ModelBuilder modelBuilder, IServiceProvider provider)
    {
        var genericType = typeof(IEntityTypeConfiguration<>);

        var configs = provider.GetServices(typeof(object)); // загружаем всё, что есть в контейнере

        foreach (var config in configs)
        {
            var configType = config.GetType();
            var interfaceType = configType
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);

            if (interfaceType == null) continue;

            var entityType = interfaceType.GetGenericArguments()[0];

            var applyConfigMethod = typeof(ModelBuilder)
                .GetMethods()
                .First(m => m.Name == nameof(ModelBuilder.ApplyConfiguration) && m.GetParameters().Length == 1)
                .MakeGenericMethod(entityType);

            applyConfigMethod.Invoke(modelBuilder, new[] { config });
        }
    }
}
