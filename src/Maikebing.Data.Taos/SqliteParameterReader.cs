// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Maikebing.Data.Taos.Properties;
using TaosPCL;

namespace Maikebing.Data.Taos
{
    internal class TaosParameterReader : TaosValueReader
    {
        private readonly string _function;
        private readonly Taos3_value[] _values;

        public TaosParameterReader(string function, Taos3_value[] values)
        {
            _function = function;
            _values = values;
        }

        public override int FieldCount
            => _values.Length;

        protected override string GetOnNullErrorMsg(int ordinal)
            => Resources.UDFCalledWithNull(_function, ordinal);

        protected override double GetDoubleCore(int ordinal)
            => raw.Taos3_value_double(_values[ordinal]);

        protected override long GetInt64Core(int ordinal)
            => raw.Taos3_value_int64(_values[ordinal]);

        protected override string GetStringCore(int ordinal)
            => raw.Taos3_value_text(_values[ordinal]);

        protected override byte[] GetBlobCore(int ordinal)
            => raw.Taos3_value_blob(_values[ordinal]);

        protected override int GetTaosType(int ordinal)
            => raw.Taos3_value_type(_values[ordinal]);
    }
}
