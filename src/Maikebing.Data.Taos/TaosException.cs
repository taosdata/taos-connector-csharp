// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;

namespace Maikebing.Data.Taos
{
    /// <summary>
    ///     Represents a Taos error.
    /// </summary>
    public class TaosException : DbException
    {
        TaosErrorResult _taosError;
        public TaosException(TaosErrorResult taosError)
      
        {
            _taosError = taosError;
        }

        /// <summary>
        ///     Gets the Taos error code.
        /// </summary>
        /// <value>The Taos error code.</value>
        /// <seealso href="http://Taos.org/rescode.html">Taos Result Codes</seealso>
        public virtual int TaosErrorCode => _taosError.code;

        public virtual string TaosStatus => _taosError.status;

        public override string Message => _taosError.desc;
        public override int ErrorCode => TaosErrorCode;
        /// <summary>
        ///     Throws an exception with a specific Taos error code value.
        /// </summary>
        /// <param name="rc">The Taos error code corresponding to the desired exception.</param>
        /// <param name="db">A handle to database connection.</param>
        /// <remarks>
        ///     No exception is thrown for non-error result codes.
        /// </remarks>
        public static void ThrowExceptionForRC(string _commandText, TaosErrorResult taosError)
        {
            var te = new TaosException(taosError);
            te.Data.Add("commandText", _commandText);
            throw te;
        }
    }
}
