// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using TaosPCL;

namespace Maikebing.Data.Taos
{
    internal class TaosResultBinder : TaosValueBinder
    {
        private readonly Taos3_context _ctx;

        public TaosResultBinder(Taos3_context ctx, object value)
            : base(value)
        {
            _ctx = ctx;
        }

        protected override void BindBlob(byte[] value)
            => raw.Taos3_result_blob(_ctx, value);

        protected override void BindDoubleCore(double value)
            => raw.Taos3_result_double(_ctx, value);

        protected override void BindInt64(long value)
            => raw.Taos3_result_int64(_ctx, value);

        protected override void BindNull()
            => raw.Taos3_result_null(_ctx);

        protected override void BindText(string value)
            => raw.Taos3_result_text(_ctx, value);
    }
}
