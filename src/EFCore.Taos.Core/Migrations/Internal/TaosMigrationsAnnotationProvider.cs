// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Taos.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Taos.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.Taos.Migrations.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosMigrationsAnnotationProvider : MigrationsAnnotationProvider
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosMigrationsAnnotationProvider([NotNull] MigrationsAnnotationProviderDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override IEnumerable<IAnnotation> For(IModel model)
        {
            if (model.GetEntityTypes().SelectMany(t => t.GetProperties()).Any(
                p => TaosTypeMappingSource.IsSpatialiteType(p.Relational().ColumnType)))
            {
                yield return new Annotation(TaosAnnotationNames.InitSpatialMetaData, true);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override IEnumerable<IAnnotation> For(IProperty property)
        {
            if (property.ValueGenerated == ValueGenerated.OnAdd
                && property.ClrType.UnwrapNullableType().IsInteger()
                && !HasConverter(property))
            {
                yield return new Annotation(TaosAnnotationNames.Autoincrement, true);
            }

            var srid = property.Taos().Srid;
            if (srid != null)
            {
                yield return new Annotation(TaosAnnotationNames.Srid, srid);
            }

            var dimension = property.Taos().Dimension;
            if (dimension != null)
            {
                yield return new Annotation(TaosAnnotationNames.Dimension, dimension);
            }
        }

        private static bool HasConverter(IProperty property)
            => (property.FindMapping()?.Converter
                ?? property.GetValueConverter()) != null;
    }
}
