// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Maikebing.EntityFrameworkCore.Taos.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Extension methods for <see cref="IProperty" /> for Taos metadata.
    /// </summary>
    public static class TaosPropertyExtensions
    {
        /// <summary>
        ///     Returns the SRID to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The SRID to use when creating a column for this property. </returns>
        public static int? GetSrid([NotNull] this IProperty property)
            => (int?)property[TaosAnnotationNames.Srid];

        /// <summary>
        ///     Sets the SRID to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <param name="value"> The SRID. </param>
        public static void SetSrid([NotNull] this IMutableProperty property, int? value)
            => property.SetOrRemoveAnnotation(TaosAnnotationNames.Srid, value);

        /// <summary>
        ///     Sets the SRID to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <param name="value"> The SRID. </param>
        /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
        public static void SetSrid([NotNull] this IConventionProperty property, int? value, bool fromDataAnnotation = false)
            => property.SetOrRemoveAnnotation(TaosAnnotationNames.Srid, value, fromDataAnnotation);

        /// <summary>
        ///     Gets the <see cref="ConfigurationSource" /> for the column SRID.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The <see cref="ConfigurationSource" /> for the column SRID. </returns>
        public static ConfigurationSource? GetSridConfigurationSource([NotNull] this IConventionProperty property)
            => property.FindAnnotation(TaosAnnotationNames.Srid)?.GetConfigurationSource();

        /// <summary>
        ///     Returns the dimension to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The dimension to use when creating a column for this property. </returns>
        public static string GetGeometricDimension([NotNull] this IProperty property)
            => (string)property[TaosAnnotationNames.Dimension];

        /// <summary>
        ///     Sets the dimension to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <param name="value"> The dimension. </param>
        public static void SetGeometricDimension([NotNull] this IMutableProperty property, [CanBeNull] string value)
            => property.SetOrRemoveAnnotation(TaosAnnotationNames.Dimension, value);

        /// <summary>
        ///     Sets the dimension to use when creating a column for this property.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <param name="value"> The dimension. </param>
        /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
        public static void SetGeometricDimension(
            [NotNull] this IConventionProperty property, [CanBeNull] string value, bool fromDataAnnotation = false)
            => property.SetOrRemoveAnnotation(TaosAnnotationNames.Dimension, value, fromDataAnnotation);

        /// <summary>
        ///     Gets the <see cref="ConfigurationSource" /> for the column dimension.
        /// </summary>
        /// <param name="property"> The property. </param>
        /// <returns> The <see cref="ConfigurationSource" /> for the column dimension. </returns>
        public static ConfigurationSource? GetGeometricDimensionConfigurationSource([NotNull] this IConventionProperty property)
            => property.FindAnnotation(TaosAnnotationNames.Dimension)?.GetConfigurationSource();
    }
}
