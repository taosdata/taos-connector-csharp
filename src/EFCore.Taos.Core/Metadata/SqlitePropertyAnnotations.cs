// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Taos.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     Properties for Taos-specific annotations accessed through
    ///     <see cref="TaosMetadataExtensions.Taos(IMutableProperty)" />.
    /// </summary>
    public class TaosPropertyAnnotations : RelationalPropertyAnnotations, ITaosPropertyAnnotations
    {
        /// <summary>
        ///     Constructs an instance for annotations of the given <see cref="IProperty" />.
        /// </summary>
        /// <param name="property"> The <see cref="IProperty" /> to use. </param>
        public TaosPropertyAnnotations([NotNull] IProperty property)
            : base(property)
        {
        }

        /// <summary>
        ///     Constructs an instance for annotations of the <see cref="IProperty" />
        ///     represented by the given annotation helper.
        /// </summary>
        /// <param name="annotations">
        ///     The <see cref="RelationalAnnotations" /> helper representing the <see cref="IProperty" /> to annotate.
        /// </param>
        protected TaosPropertyAnnotations([NotNull] RelationalAnnotations annotations)
            : base(annotations)
        {
        }

        /// <summary>
        ///     Gets or sets the SRID to use when creating a column for this property.
        /// </summary>
        public virtual int? Srid
        {
            get => (int?)Annotations.Metadata[TaosAnnotationNames.Srid];
            set => SetSrid(value);
        }

        /// <summary>
        ///     Sets the SRID to use when creating a column for this property.
        /// </summary>
        /// <param name="value"> The SRID. </param>
        /// <returns> true if the annotation was set; otherwise, false. </returns>
        protected virtual bool SetSrid(int? value)
            => Annotations.SetAnnotation(TaosAnnotationNames.Srid, value);

        /// <summary>
        ///     Gets or sets the dimension to use when creating a column for this property.
        /// </summary>
        public virtual string Dimension
        {
            get => (string)Annotations.Metadata[TaosAnnotationNames.Dimension];
            set => SetDimension(value);
        }

        /// <summary>
        ///     Sets the dimension to use when creating a column for this property.
        /// </summary>
        /// <param name="value"> The dimension. </param>
        /// <returns> true if the annotation was set; otherwise, false. </returns>
        protected virtual bool SetDimension(string value)
            => Annotations.SetAnnotation(TaosAnnotationNames.Dimension, value);
    }
}
