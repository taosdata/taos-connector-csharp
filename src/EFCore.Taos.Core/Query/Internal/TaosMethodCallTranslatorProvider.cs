// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query;

namespace IoTSharp.EntityFrameworkCore.Taos.Query.Internal
{
    public class TaosMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
    {
        public TaosMethodCallTranslatorProvider([NotNull] RelationalMethodCallTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
            var sqlExpressionFactory = dependencies.SqlExpressionFactory;

            AddTranslators(
                new IMethodCallTranslator[]
                {
                    new TaosMathTranslator(sqlExpressionFactory),
                    new TaosDateTimeAddTranslator(sqlExpressionFactory),
                    new TaosStringMethodTranslator(sqlExpressionFactory)
                });
        }
    }
}
