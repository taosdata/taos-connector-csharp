// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Taos.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Taos.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Taos.Query.ExpressionTranslators.Internal;
using Microsoft.EntityFrameworkCore.Taos.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Taos.Query.Internal;
using Microsoft.EntityFrameworkCore.Taos.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Taos.Storage.Internal;
using Microsoft.EntityFrameworkCore.Taos.Update.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Taos specific extension methods for <see cref="IServiceCollection" />.
    /// </summary>
    public static class TaosServiceCollectionExtensions
    {
        /// <summary>
        ///     <para>
        ///         Adds the services required by the Taos database provider for Entity Framework
        ///         to an <see cref="IServiceCollection" />. You use this method when using dependency injection
        ///         in your application, such as with ASP.NET. For more information on setting up dependency
        ///         injection, see http://go.microsoft.com/fwlink/?LinkId=526890.
        ///     </para>
        ///     <para>
        ///         You only need to use this functionality when you want Entity Framework to resolve the services it uses
        ///         from an external dependency injection container. If you are not using an external
        ///         dependency injection container, Entity Framework will take care of creating the services it requires.
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        ///           public void ConfigureServices(IServiceCollection services)
        ///           {
        ///               var connectionString = "connection string to database";
        /// 
        ///               services
        ///                   .AddEntityFrameworkTaos()
        ///                   .AddDbContext&lt;MyContext&gt;((serviceProvider, options) =>
        ///                       options.UseTaos(connectionString)
        ///                              .UseInternalServiceProvider(serviceProvider));
        ///           }
        ///       </code>
        /// </example>
        /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <returns>
        ///     The same service collection so that multiple calls can be chained.
        /// </returns>
        public static IServiceCollection AddEntityFrameworkTaos([NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                .TryAdd<IDatabaseProvider, DatabaseProvider<TaosOptionsExtension>>()
                .TryAdd<IRelationalTypeMappingSource, TaosTypeMappingSource>()
                .TryAdd<ISqlGenerationHelper, TaosSqlGenerationHelper>()
                .TryAdd<IMigrationsAnnotationProvider, TaosMigrationsAnnotationProvider>()
                .TryAdd<IModelValidator, TaosModelValidator>()
                .TryAdd<IConventionSetBuilder, TaosConventionSetBuilder>()
                .TryAdd<IUpdateSqlGenerator, TaosUpdateSqlGenerator>()
                .TryAdd<ISingletonUpdateSqlGenerator, TaosUpdateSqlGenerator>()
                .TryAdd<IModificationCommandBatchFactory, TaosModificationCommandBatchFactory>()
                .TryAdd<IRelationalConnection>(p => p.GetService<ITaosRelationalConnection>())
                .TryAdd<IMigrationsSqlGenerator, TaosMigrationsSqlGenerator>()
                .TryAdd<IRelationalDatabaseCreator, TaosDatabaseCreator>()
                .TryAdd<IHistoryRepository, TaosHistoryRepository>()
                .TryAdd<IMemberTranslator, TaosCompositeMemberTranslator>()
                .TryAdd<ICompositeMethodCallTranslator, TaosCompositeMethodCallTranslator>()
                .TryAdd<IQuerySqlGeneratorFactory, TaosQuerySqlGeneratorFactory>()
                .TryAdd<ISqlTranslatingExpressionVisitorFactory, TaosSqlTranslatingExpressionVisitorFactory>()
                .TryAdd<IRelationalResultOperatorHandler, TaosResultOperatorHandler>()
                .TryAddProviderSpecificServices(
                    b => b.TryAddScoped<ITaosRelationalConnection, TaosRelationalConnection>());

            builder.TryAddCoreServices();

            return serviceCollection;
        }
    }
}
