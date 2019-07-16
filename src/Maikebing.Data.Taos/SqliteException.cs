// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using Maikebing.Data.Taos.Properties;
using TaosPCL;

namespace Maikebing.Data.Taos
{
    /// <summary>
    ///     Represents a Taos error.
    /// </summary>
    public class TaosException : DbException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosException" /> class.
        /// </summary>
        /// <param name="message">The message to display for the exception. Can be null.</param>
        /// <param name="errorCode">The Taos error code.</param>
        public TaosException(string message, int errorCode)
            : this(message, errorCode, errorCode)
        { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TaosException" /> class.
        /// </summary>
        /// <param name="message">The message to display for the exception. Can be null.</param>
        /// <param name="errorCode">The Taos error code.</param>
        /// /// <param name="extendedErrorCode">The extended Taos error code.</param>
        public TaosException(string message, int errorCode, int extendedErrorCode)
            : base(message)
        {
            TaosErrorCode = errorCode;
            TaosExtendedErrorCode = extendedErrorCode;
        }

        /// <summary>
        ///     Gets the Taos error code.
        /// </summary>
        /// <value>The Taos error code.</value>
        /// <seealso href="http://Taos.org/rescode.html">Taos Result Codes</seealso>
        public virtual int TaosErrorCode { get; }

        /// <summary>
        ///     Gets the extended Taos error code.
        /// </summary>
        /// <value>The Taos error code.</value>
        /// <seealso href="https://Taos.org/rescode.html#extrc">Taos Result Codes</seealso>
        public virtual int TaosExtendedErrorCode { get; }

        /// <summary>
        ///     Throws an exception with a specific Taos error code value.
        /// </summary>
        /// <param name="rc">The Taos error code corresponding to the desired exception.</param>
        /// <param name="db">A handle to database connection.</param>
        /// <remarks>
        ///     No exception is thrown for non-error result codes.
        /// </remarks>
        public static void ThrowExceptionForRC(int rc, Taos3 db)
        {
            if (rc == raw.Taos_OK
                || rc == raw.Taos_ROW
                || rc == raw.Taos_DONE)
            {
                return;
            }

            string message;
            int extendedErrorCode;
            if (db == null || db.ptr == IntPtr.Zero || rc != raw.Taos3_errcode(db))
            {
                message = raw.Taos3_errstr(rc) + " " + Resources.DefaultNativeError;
                extendedErrorCode = rc;
            }
            else
            {
                message = raw.Taos3_errmsg(db);
                extendedErrorCode = raw.Taos3_extended_errcode(db);
            }

            throw new TaosException(Resources.TaosNativeError(rc, message), rc, extendedErrorCode);
        }
    }
}
