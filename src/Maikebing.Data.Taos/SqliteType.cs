// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using TaosPCL;

namespace Maikebing.Data.Taos
{
    /// <summary>
    ///     Represents the type affinities used by columns in Taos tables.
    /// </summary>
    /// <seealso href="http://Taos.org/datatype3.html">Datatypes In Taos Version 3</seealso>
    public enum TaosType
    {
        /// <summary>
        ///     A signed integer.
        /// </summary>
        Integer = raw.Taos_INTEGER,

        /// <summary>
        ///     A floating point value.
        /// </summary>
        Real = raw.Taos_FLOAT,

        /// <summary>
        ///     A text string.
        /// </summary>
        Text = raw.Taos_TEXT,

        /// <summary>
        ///     A blob of data.
        /// </summary>
        Blob = raw.Taos_BLOB
    }
}
