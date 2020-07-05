// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Taos specific extension methods for <see cref="PropertyBuilder" />.
    /// </summary>
    public static class TaosPropertyBuilderExtensions
    {
        /// <summary>
        ///     Configures the SRID of the column that the property maps to when targeting Taos.
        /// </summary>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="srid"> The SRID. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder ForTaosHasSrid([NotNull] this PropertyBuilder propertyBuilder, int srid)
        {
            Check.NotNull(propertyBuilder, nameof(propertyBuilder));

            propertyBuilder.Metadata.Taos().Srid = srid;

            return propertyBuilder;
        }

        /// <summary>
        ///     Configures the SRID of the column that the property maps to when targeting Taos.
        /// </summary>
        /// <param name="propertyBuilder"> The builder for the property being configured. </param>
        /// <param name="srid"> The SRID. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public static PropertyBuilder<TProperty> ForTaosHasSrid<TProperty>(
            [NotNull] this PropertyBuilder<TProperty> propertyBuilder,
            int srid)
            => (PropertyBuilder<TProperty>)ForTaosHasSrid((PropertyBuilder)propertyBuilder, srid);
    }
}
