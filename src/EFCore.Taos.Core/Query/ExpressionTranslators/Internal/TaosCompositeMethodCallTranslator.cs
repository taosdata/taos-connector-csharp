// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace Microsoft.EntityFrameworkCore.Taos.Query.ExpressionTranslators.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosCompositeMethodCallTranslator : RelationalCompositeMethodCallTranslator
    {
        private static readonly IMethodCallTranslator[] _TaosTranslators =
        {
            new TaosContainsOptimizedTranslator(),
            new TaosDateTimeAddTranslator(),
            new TaosEndsWithOptimizedTranslator(),
            new TaosMathTranslator(),
            new TaosStartsWithOptimizedTranslator(),
            new TaosStringIsNullOrWhiteSpaceTranslator(),
            new TaosStringToLowerTranslator(),
            new TaosStringToUpperTranslator(),
            new TaosStringTrimEndTranslator(),
            new TaosStringTrimStartTranslator(),
            new TaosStringTrimTranslator(),
            new TaosStringIndexOfTranslator(),
            new TaosStringReplaceTranslator(),
            new TaosStringSubstringTranslator()
        };

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosCompositeMethodCallTranslator(
            [NotNull] RelationalCompositeMethodCallTranslatorDependencies dependencies)
            : base(dependencies)
        {
            AddTranslators(_TaosTranslators);
        }
    }
}
