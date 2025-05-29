Это LimsDbContext:
#pragma warning disable SA1124

namespace Lims.Infrastructure.Database.Contexts;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lims.Core.Domain.Abstract;
using Lims.Core.Domain.Base.Exceptions;
using Lims.Core.Domain.Base.Extensions;
using Lims.Core.Domain.Entities;
using Lims.Core.Domain.Entities.Abstract;
using Lims.Core.Domain.Entities.Temp;
using Lims.Core.Domain.Glossaries;
using Lims.Core.UseCases.Admin.Users.Abstract;
using Lims.Infrastructure.Common.Extensions;
using Lims.Infrastructure.Database.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

public class LimsDbContext : DbContext, ILimsDbContext
{
    public LimsDbContext(DbContextOptions<LimsDbContext> options)
        : base(options)
    {
        this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public virtual DbSet<MngtWorker> MngtWorkersTemp { get; set; } = null!;

    public virtual DbSet<ObjField> ObjFieldsTemp { get; set; } = null!;

    public virtual DbSet<ObjWell> ObjWellsTemp { get; set; } = null!;

    public virtual DbSet<ObjGouge> ObjGougesTemp { get; set; } = null!;

    #region DbSets

    public virtual DbSet<CollectionObjectEntity> CollectionObjects { get; set; } = null!;

    public virtual DbSet<LocationWellEntity> LocationWells { get; set; } = null!;

    public virtual DbSet<LocationFieldEntity> LocationFields { get; set; } = null!;

    public virtual DbSet<ObjMainEntity> ObjMains { get; set; } = null!;

    public virtual DbSet<ObjCollectionEntity> ObjCollections { get; set; } = null!;

    public virtual DbSet<ObjGougeEntity> ObjGouges { get; set; } = null!;

    public virtual DbSet<ObjMicrosectionEntity> ObjMicrosections { get; set; } = null!;

    public virtual DbSet<ObjSampleEntity> ObjSamples { get; set; } = null!;

    public virtual DbSet<ObjSaturationEntity> ObjSaturations { get; set; } = null!;

    public virtual DbSet<ObjSludgeEntity> ObjSludges { get; set; } = null!;

    public virtual DbSet<ObjStratumEntity> ObjStratums { get; set; } = null!;

    public DbSet<ObjFluidEntity> ObjFluids { get; set; } = null!;

    public DbSet<ObjFluidParentEntity> ObjFluidParents { get; set; } = null!;

    public virtual DbSet<StratumCodeEntity> StratumCodes { get; set; } = null!;

    public virtual DbSet<MngtDepartmentEntity> MngtDepartments { get; set; } = null!;

    public virtual DbSet<MngtPostEntity> MngtPosts { get; set; } = null!;

    public virtual DbSet<MngtWorkerEntity> MngtWorkers { get; set; } = null!;

    public virtual DbSet<PrcsStageEntity> PrcsStages { get; set; } = null!;

    public virtual DbSet<PrcsMethodEntity> PrcsMethods { get; set; } = null!;

    public virtual DbSet<PrcsBranchEntity> PrcsBranches { get; set; } = null!;

    public virtual DbSet<PrcsBranchMethodEntity> PrcsBranchMethods { get; set; } = null!;

    public virtual DbSet<PrcsOrderEntity> PrcsOrders { get; set; } = null!;

    public virtual DbSet<PrcsTaskEntity> PrcsTasks { get; set; } = null!;

    public virtual DbSet<PrcsRulePermissionEntity> PrcsRulePermissions { get; set; } = null!;

    public virtual DbSet<PrcsTemplateEntity> PrcsTemplates { get; set; } = null!;

    public virtual DbSet<PrcsTemplateContentEntity> PrcsTemplateContents { get; set; } = null!;

    public virtual DbSet<PrcsFlowEntity> PrcsFlows { get; set; } = null!;

    public DbSet<PhotoStorageEntity> PhotoStorages { get; set; } = null!;

    public DbSet<PhotoSweepEntity> PhotoSweeps { get; set; } = null!;

    public DbSet<PhotoSweepPartEntity> PhotoSweepParts { get; set; } = null!;

    public DbSet<PhotoCoreEntity> PhotoCores { get; set; } = null!;

    public DbSet<PhotoCoreSourceEntity> PhotoCoreSources { get; set; }

    public DbSet<PhotoCoreSegmentEntity> PhotoCoreSegments { get; set; } = null!;

    public DbSet<PhotoSampleCubeEntity> PhotoSampleCubes { get; set; } = null!;

    public DbSet<PhotoSampleCubeSourceEntity> PhotoSampleCubeSources { get; set; }

    public DbSet<PhotoSampleCubeSegmentEntity> PhotoSampleCubeSegments { get; set; } = null!;

    public DbSet<SysSequenceEntity> SysSequences { get; set; } = null!;

    public DbSet<StrgComingEntity> StrgComings { get; set; } = null!;

    public DbSet<StrgComingObjectEntity> StrgComingObjects { get; set; } = null!;

    public DbSet<DictProviderEntity> DictProviders { get; set; } = null!;

    public DbSet<MngtWorkerComingType> MngtWorkerComingTypes { get; set; } = null!;

    public DbSet<MngtWorkerStageEntity> MngtWorkerStages { get; set; } = null!;

    #endregion

    public async Task<long> EnsureDeveloperWorkerCreatedAsync()
    {
        if (this.MngtWorkers.Any(e => e.Id == 1 && e.FirstName != IUserService.DeveloperName))
        {
            throw new LimsOperationException("Невозможно создать рабочего для главной учётной записи, т.к. пользователь с идентификатором 1 уже существует.");
        }

        var worker = await this.MngtWorkers
            .AsTracking()
            .Include(e => e.ComingTypes)
            .SingleOrDefaultAsync(e => e.Id == 1 && e.FirstName == IUserService.DeveloperName);

        if (worker == null)
        {
            worker = new MngtWorkerEntity
            {
                Id = 1,
                FirstName = IUserService.DeveloperName,
                LastName = IUserService.DeveloperName,
                Status = true,
                DateTimeCreation = DateTime.Now,
                ComingTypes = new List<MngtWorkerComingType>(),
            };

            this.AddEntity(worker);
        }

        var comingTypes = Enum.GetValues<ComingTypes>().ToList();
        comingTypes.Remove(ComingTypes.None);

        foreach (var comingType in comingTypes)
        {
            var dateTimeCreation = DateTime.Now;
            if (!worker.ComingTypes.Any(e => e.ComingType == comingType))
            {
                worker.ComingTypes.Add(
                    new MngtWorkerComingType
                    {
                        Id = default,
                        ComingType = comingType,
                        DateTimeCreation = dateTimeCreation,
                        WorkerId = worker.Id,
                        Worker = worker,
                    });
            }
        }

        await this.SaveChangesAndThrowAsync();

        return worker.Id;
    }

    public IQueryable<TEntity> FromSQL<TEntity>(FormattableString sql)
        where TEntity : class
    {
        return this.GetDbSet<TEntity>().FromSql(sql);
    }

    public async Task<object?> FindByIdAsync(Type entityType, long id)
    {
        return await this.FindAsync(entityType, id);
    }

    public DbSet<TEntity> GetDbSet<TEntity>()
         where TEntity : class
    {
        return this.Set<TEntity>();
    }

    public List<TEntity> SetEntities<TEntity>(IEnumerable<long> ids)
        where TEntity : BaseEntity
    {
        return this.Set<TEntity>()
            .Where(e => ids.Contains(e.Id))
            .ToList();
    }

    public void AddEntity<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        this.Add(entity);
    }

    public List<TEntity> GetTrackedEntities<TEntity>()
        where TEntity : BaseEntity
    {
        return this.ChangeTracker.Entries<TEntity>()
            .Select(e => e.Entity)
            .ToList();
    }

    public void ClearTrackedEntities()
    {
        this.ChangeTracker.Clear();
    }

    public void LoadProperty(BaseEntity entity, string property)
    {
        this.Entry(entity)
            .Reference(property)
            .Load();
    }

    public void Update(BaseEntity entity)
    {
        this.Update<BaseEntity>(entity);
    }

    public void SetModifiedProperties(BaseEntity entity, IEnumerable<string> properties)
    {
        var entry = this.Entry(entity);

        // Если хотя бы одно поле сущности входит в множество properties,
        // то её состояние меняется на Modified иначе Unchanged.
        entry.State = entry.Properties
            .Select(p => p.Metadata.Name)
            .Any(n => properties.Contains(n)) ?
            EntityState.Modified :
            EntityState.Unchanged;

        entry.Properties.ToList().ForEach(p => p.IsModified = properties.Contains(p.Metadata.Name));
    }

    public void SetUnchangedProperties(BaseEntity entity, IEnumerable<string> properties)
    {
        var entry = this.Entry(entity);

        // Если все поля сущности входят в множество properties,
        // то её состояние меняется на Unchanged иначе Modified.
        entry.State = entry.Properties
            .Select(p => p.Metadata.Name)
            .All(n => properties.Contains(n)) ?
            EntityState.Unchanged :
            EntityState.Modified;

        entry.Properties.ToList().ForEach(p => p.IsModified = !properties.Contains(p.Metadata.Name));
    }

    public string GetObjectSubtype<TObjEntity>()
        where TObjEntity : BaseObjectEntity
    {
        return EntityTypeBuilderExtension.GetTableName<TObjEntity>();
    }

    public void Reload<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        this.Entry(entity).Reload();
    }

    public void AttachEntity(object entity)
    {
        this.Attach(entity);
    }

    public void AttachEntity<TEntity>(TEntity entity)
        where TEntity : BaseEntity
    {
        this.Attach(entity);
    }

    public void AttachEntities<TEntity>(List<TEntity> entities)
        where TEntity : BaseEntity
    {
        this.AttachRange(entities);
    }

    public async Task ContainsAndThrowAsync<TEntity>(IEnumerable<long> ids)
        where TEntity : BaseEntity
    {
        var foundedIds = await this.Set<TEntity>()
            .Where(e => ids.Contains(e.Id))
            .AsNoTracking()
            .Select(e => e.Id)
            .ToListAsync();

        var notFoundedIds = ids.Where(id => !foundedIds.Contains(id));

        if (notFoundedIds.Any())
        {
            throw new LimsNotFoundException(typeof(TEntity), notFoundedIds.ToArray());
        }
    }

    public async Task AnyAndThrowAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
        where TEntity : class
    {
        var exist = await this.Set<TEntity>()
            .AsNoTracking()
            .AnyAsync(predicate, cancellationToken);

        if (!exist)
        {
            throw new LimsNotFoundException($"Объекты типа {typeof(TEntity).Name} не найдены. Предикат: {predicate}");
        }
    }

    public async Task<List<string>> GetCommentOfLinkingTablesAsync<T>(long id, CancellationToken cancellationToken)
        where T : BaseEntity
    {
        var foreignKeys = this.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(T))
            ?.GetReferencingForeignKeys();

        if (foreignKeys == null)
        {
            throw new ArgumentException($"Не найден тип сущности для CLR типа {nameof(T)}");
        }

        var entity = await this.GetDbSet<T>()
            .AsNoTracking()
            .Where(e => e.Id == id)
            .IncludeLinkingProperties(this.Model)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            throw new LimsNotFoundException(typeof(T), new long[] { id });
        }

        var comments = new List<string>();
        foreach (var fk in foreignKeys)
        {
            var propName = fk.PrincipalToDependent?.Name;
            if (propName == null)
            {
                continue;
            }

            var comment = fk.DeclaringEntityType.GetComment() ?? "Отсутствует описание";
            var value = typeof(T).GetProperty(propName)?.GetValue(entity);
            var valueAsCollection = value as IEnumerable;

            if ((value != null && valueAsCollection == null) || valueAsCollection?.Cast<object>().Any() == true)
            {
                comments.Add(comment);
            }
        }

        return comments;
    }

    public async Task<List<string>> GetCommentOfLinkingTablesAsync(Type entityType, long id, DeleteBehavior[] excludeDeleteBehavior, CancellationToken cancellationToken)
    {
        var foreignKeys = this.GetService<IDesignTimeModel>().Model
            .FindEntityType(entityType)
            ?.GetReferencingForeignKeys()
            .Where(fk => !excludeDeleteBehavior.Contains(fk.DeleteBehavior));

        if (foreignKeys == null)
        {
            throw new ArgumentException($"Не найден тип сущности для CLR типа {entityType.Name}");
        }

        var entity = (await this.FindAsync(entityType, id)) as BaseEntity;

        if (entity == null)
        {
            throw new LimsNotFoundException(entityType, new long[] { id });
        }

        var trackedEntities = this.ChangeTracker.Entries().Select(e => e.Entity).ToList();
        this.ClearTrackedEntities();

        foreach (var navigation in foreignKeys.Select(e => e.PrincipalToDependent))
        {
            if (navigation == null)
            {
                continue;
            }

            if (navigation.IsCollection)
            {
                this.Attach(entity)
                    .Collection(navigation.Name)
                    .Load();
            }
            else
            {
                this.Attach(entity)
                    .Reference(navigation.Name)
                    .Load();
            }
        }

        this.ClearTrackedEntities();
        this.AttachRange(trackedEntities);

        var comments = new List<string>();
        foreach (var fk in foreignKeys)
        {
            var propName = fk.PrincipalToDependent?.Name;
            if (propName == null)
            {
                continue;
            }

            var comment = fk.DeclaringEntityType.GetComment() ?? "Отсутствует описание";
            var value = entityType.GetProperty(propName)?.GetValue(entity);
            var valueAsCollection = value as IEnumerable;

            if ((value != null && valueAsCollection == null) || valueAsCollection?.Cast<object>().Any() == true)
            {
                comments.Add(comment);
            }
        }

        return comments;
    }

    public string? GetEntityComment<T>()
        where T : BaseEntity
    {
        var entityType = this.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(T));
        if (entityType == null)
        {
            throw new ArgumentException($"Не найден тип сущности для CLR типа {nameof(T)}");
        }

        return entityType.GetComment();
    }

    public string? GetEntityComment(Type entityType)
    {
        var modelEntityType = this.GetService<IDesignTimeModel>().Model.FindEntityType(entityType);
        if (modelEntityType == null)
        {
            throw new ArgumentException($"Не найден тип сущности для CLR типа {entityType.Name}");
        }

        return modelEntityType.GetComment();
    }

    public async Task<IDbContextTransaction> StartTransaction(CancellationToken cancellationToken)
    {
        return await this.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAndThrowAsync(CancellationToken cancellationToken = default)
    {
        return await this.SaveChangesAndThrowAsyncExtension(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var provider = ((IInfrastructure<IServiceProvider>)this).Instance;

        modelBuilder.HasPostgresExtension("dblink")
                    .HasAnnotation("Relational:Collation", "ru_RU.utf8");
        modelBuilder.HasDefaultSchema(LimsDbConstants.DataSchema);
        modelBuilder.ApplyAllConfigurationsFromServices(provider);
    }
}

Это фабрика для создания контекста со своим ServiceCollection:

#pragma warning disable SA1009
namespace Lims.Infrastructure.Common.Helpers;

using System.Reflection;
using Lims.Core.Domain.Base.Exceptions;
using Lims.Infrastructure.Common.Abstract;
using Lims.Infrastructure.Common.Enums;
using Lims.Infrastructure.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public class DbContextFactory : IDbContextFactory
{
    private readonly IServiceCollection services = new ServiceCollection();

    public void AddInMemory()
    {
        this.services.AddEntityFrameworkInMemoryDatabase();
        this.services.AddEntityFrameworkNamingConventions();
    }

    public void AddNpgsql()
    {
        this.services.AddEntityFrameworkNpgsql();
        this.services.AddEntityFrameworkNamingConventions();
    }

    public void AddInjections(Assembly configAssembly, List<ServiceTypeInfo> injections)
    {
        this.services.AddEntityTypeConfigs(configAssembly);

        foreach (var injection in injections)
        {
            switch (injection.InjectionType)
            {
                case InjectionTypes.Scoped:
                    AddInjectScoped(this.services, injection);
                    break;
                case InjectionTypes.Singleton:
                    AddInjectSingleton(this.services, injection);
                    break;
                default:
                    throw new LimsOperationException();
            }
        }
    }

    public TContext CreateContext<TContext>(
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        this.services.AddDbContext<TContext>((provider, options) =>
        {
            options.UseInternalServiceProvider(provider);
            optionsAction(provider, options);
        });

        var provider = this.services.BuildServiceProvider();

        return provider.GetService<TContext>()!;
    }

    private static void AddInjectScoped(IServiceCollection services, ServiceTypeInfo injection)
    {
        if (injection.ServiceInterface == null)
        {
            services.AddScoped(injection.ServiceType);
        }

        if (injection.ServiceInterface != null)
        {
            services.AddScoped(injection.ServiceInterface, injection.ServiceType);
        }
    }

    private static void AddInjectSingleton(IServiceCollection services, ServiceTypeInfo injection)
    {
        if (injection.ServiceInterface == null)
        {
            services.AddSingleton(injection.ServiceType);
        }

        if (injection.ServiceInterface != null)
        {
            services.AddSingleton(injection.ServiceInterface, injection.ServiceType);
        }
    }
}

Это для миграций:
namespace Lims.Infrastructure.Database.Contexts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using Lims.Core.Domain.Abstract;
    using Lims.Core.Domain.Base.Exceptions;
    using Lims.Infrastructure.Common.Abstract;
    using Lims.Infrastructure.Common.Enums;
    using Lims.Infrastructure.Common.Extensions;
    using Lims.Infrastructure.Common.Helpers;
    using Lims.Infrastructure.Database.Constants;
    using Lims.Infrastructure.Database.Extensions;
    using Lims.Infrastructure.Database.Helpers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Npgsql;

    public class LimsDbContextFactory : IDesignTimeDbContextFactory<LimsDbContext>
    {
        public LimsDbContext CreateDbContext(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var configBuilder = new ConfigurationBuilder()
                .AddUserSecrets(assembly);

            var config = configBuilder.Build();
            var connectionString = config["ConnectionStrings:PostgreSQL"]
                ?? throw new LimsOperationException("Строка подключения не найдена.");
            var injections = new List<ServiceTypeInfo>
            {
                new ServiceTypeInfo(InjectionTypes.Scoped, typeof(ObjectGlossary), typeof(IObjectGlossary)),
            };

            var dbContextFactory = new DbContextFactory();
            dbContextFactory.AddNpgsql();
            dbContextFactory.AddInjections(assembly, injections);

            return dbContextFactory.CreateContext<LimsDbContext>(
            (provider, options) =>
            {
                options.UseNpgsql(new NpgsqlDataSourceBuilder(connectionString).Build(), options =>
                {
                    options.SetPostgresVersion(15, 3);
                    options.MigrationsHistoryTable(LimsDbConstants.MigrationHistoryTable, schema: LimsDbConstants.MigrationSchema);
                })
                .UseSnakeCaseNamingConvention();
            });
        }
    }
}

Для приложения веб-API (ASP.NET Core)

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
namespace Lims.Infrastructure.Database.Extensions;

using System;
using System.Collections.Generic;
using System.Reflection;
using Lims.Core.Domain.Abstract;
using Lims.Core.Domain.Base.Exceptions;
using Lims.Infrastructure.Common.Abstract;
using Lims.Infrastructure.Common.Enums;
using Lims.Infrastructure.Common.Extensions;
using Lims.Infrastructure.Common.Helpers;
using Lims.Infrastructure.Database.Constants;
using Lims.Infrastructure.Database.Contexts;
using Lims.Infrastructure.Database.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

public static class ServiceCollectionExtension
{
    public static void AddLimsInfrastructure(this IServiceCollection services, string connectionString)
    {
        var configAssembly = Assembly.GetAssembly(typeof(ServiceCollectionExtension))
            ?? throw new LimsOperationException("Сборка с конфигурациями сущностей не найдена.");

        services.AddScoped<IObjectGlossary, ObjectGlossary>();
        services.AddSingleton<IDbContextFactory, DbContextFactory>();
        services.AddScoped<ILimsDbContext>(provider =>
        {
            var contextFactory = provider.GetService<IDbContextFactory>()!;
            var injections = new List<ServiceTypeInfo>
            {
                new ServiceTypeInfo(InjectionTypes.Scoped, typeof(ObjectGlossary), typeof(IObjectGlossary)),
            };

            contextFactory.AddNpgsql();
            contextFactory.AddInjections(configAssembly, injections);

            return contextFactory.CreateContext<LimsDbContext>(
            (provider, options) =>
            {
                options.UseNpgsql(new NpgsqlDataSourceBuilder(connectionString).Build(), options =>
                {
                    options.SetPostgresVersion(15, 3);
                    options.MigrationsHistoryTable(LimsDbConstants.MigrationHistoryTable, schema: LimsDbConstants.MigrationSchema);
                })
                .UseSnakeCaseNamingConvention();
            });
        });
    }
}

Это для создания LimsDbContext или ILimsDbContext инстанса через AutoFixture:

namespace Lims.Core.Tests.Common.Specimens;

using System;
using System.Collections.Generic;
using System.Reflection;
using AutoFixture.Kernel;
using Lims.Core.Domain.Abstract;
using Lims.Core.Tests.Common.Helpers;
using Lims.Infrastructure.Common.Abstract;
using Lims.Infrastructure.Common.Enums;
using Lims.Infrastructure.Common.Helpers;
using Lims.Infrastructure.Database.Contexts;
using Lims.Infrastructure.Database.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class LimsDbContextSpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request as Type == typeof(LimsDbContext) ||
            request as Type == typeof(ILimsDbContext))
        {
            var databaseName = $"{GetType().Name}{Guid.NewGuid()}";
            var configAssembly = Assembly.GetAssembly(typeof(LimsDbContext));
            var injections = new List<ServiceTypeInfo>
            {
                new ServiceTypeInfo(InjectionTypes.Scoped, typeof(ObjectGlossary), typeof(IObjectGlossary)),
            };

            var dbContextFactory = new DbContextFactory();
            dbContextFactory.AddInMemory();
            dbContextFactory.AddInjections(configAssembly, injections);

            return dbContextFactory.CreateContext<LimsDbContext>(
                (provider, options) =>
                {
                    options.UseInMemoryDatabase(databaseName)
                           .ConfigureWarnings(wc => wc.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
        }

        return new NoSpecimen();
    }
}

Почему-то в веб-API работает, когда запускаю приложение, всё ок, и запросы к БД работают, т.е. LimsDbContext создаётся нормально, но в тестах он его не может создать.

В тестак вот такая ошибка:

AutoFixture.ObjectCreationExceptionWithPath : AutoFixture was unable to create an instance from Lims.Core.Domain.Abstract.ILimsDbContext because creation unexpectedly failed with exception. Please refer to the inner exception to investigate the root cause of the failure.

Request path:
	Lims.Core.Domain.Abstract.ILimsDbContext

Inner exception messages:
	System.NullReferenceException: Object reference not set to an instance of an object.

Падает в LimsDbContextSpecimenBuilder на вот этом вызове:

 return dbContextFactory.CreateContext<LimsDbContext>(
                (provider, options) =>
                {
                    options.UseInMemoryDatabase(databaseName)
                           .ConfigureWarnings(wc => wc.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
