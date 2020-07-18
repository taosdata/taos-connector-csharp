// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Maikebing.EntityFrameworkCore.Taos.Diagnostics.Internal;
using Maikebing.EntityFrameworkCore.Taos.Scaffolding.Internal;
using Maikebing.EntityFrameworkCore.Taos.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Maikebing.EntityFrameworkCore.Taos.Design.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class TaosDesignTimeServices : IDesignTimeServices
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
            => serviceCollection
                .AddSingleton<LoggingDefinitions, TaosLoggingDefinitions>()
                .AddSingleton<IRelationalTypeMappingSource, TaosTypeMappingSource>()
                .AddSingleton<IDatabaseModelFactory, TaosDatabaseModelFactory>()
                .AddSingleton<IProviderConfigurationCodeGenerator, TaosCodeGenerator>()
                .AddSingleton<IAnnotationCodeGenerator, AnnotationCodeGenerator>();
    }
}
