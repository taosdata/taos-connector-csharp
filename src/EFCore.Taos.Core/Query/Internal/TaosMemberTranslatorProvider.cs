// Copyright (c)  Maikebing. All rights reserved.
// Licensed under the MIT License, See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Query;

namespace IoTSharp.EntityFrameworkCore.Taos.Query.Internal
{
    public class TaosMemberTranslatorProvider : RelationalMemberTranslatorProvider
    {
        public TaosMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
            var sqlExpressionFactory = dependencies.SqlExpressionFactory;

            AddTranslators(
                new IMemberTranslator[]
                {
                    new TaosDateTimeMemberTranslator(sqlExpressionFactory), new TaosStringLengthTranslator(sqlExpressionFactory)
                });
        }
    }
}
