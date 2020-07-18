// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Reflection;
using JetBrains.Annotations;
using Maikebing.EntityFrameworkCore.Taos.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Migrations
{
    /// <summary>
    ///     Taos specific extension methods for <see cref="MigrationBuilder" />.
    /// </summary>
    public static class TaosMigrationBuilderExtensions
    {
        /// <summary>
        ///     <para>
        ///         Returns true if the database provider currently in use is the Taos provider.
        ///     </para>
        /// </summary>
        /// <param name="migrationBuilder">
        ///     The migrationBuilder from the parameters on <see cref="Migration.Up(MigrationBuilder)" /> or
        ///     <see cref="Migration.Down(MigrationBuilder)" />.
        /// </param>
        /// <returns> True if Taos is being used; false otherwise. </returns>
        public static bool IsTaos([NotNull] this MigrationBuilder migrationBuilder)
            => string.Equals(
                migrationBuilder.ActiveProvider,
                typeof(TaosOptionsExtension).GetTypeInfo().Assembly.GetName().Name,
                StringComparison.Ordinal);
    }
}
