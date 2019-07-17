using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Maikebing.EntityFrameworkCore.Taos.Metadata.Conventions
{
    public class TaosDbFunctionConvention : RelationalDbFunctionConvention
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void ApplyCustomizations(InternalModelBuilder modelBuilder, string name, Annotation annotation)
        {
            base.ApplyCustomizations(modelBuilder, name, annotation);
         
            ((DbFunction)annotation.Value).DefaultSchema = "dbo";
        }
    }
}
