// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    ///     Taos specific extension methods for metadata.
    /// </summary>
    public static class TaosMetadataExtensions
    {
        /// <summary>
        ///     Gets the Taos specific metadata for a property.
        /// </summary>
        /// <param name="property"> The property to get metadata for. </param>
        /// <returns> The Taos specific metadata for the property. </returns>
        public static TaosPropertyAnnotations Taos([NotNull] this IMutableProperty property)
            => (TaosPropertyAnnotations)Taos((IProperty)property);

        /// <summary>
        ///     Gets the Taos specific metadata for a property.
        /// </summary>
        /// <param name="property"> The property to get metadata for. </param>
        /// <returns> The Taos specific metadata for the property. </returns>
        public static ITaosPropertyAnnotations Taos([NotNull] this IProperty property)
            => new TaosPropertyAnnotations(Check.NotNull(property, nameof(property)));
    }
}
