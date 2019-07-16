// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Maikebing.Data.Taos
{
    internal static class TaosConnectionExtensions
    {
        public static int ExecuteNonQuery(
            this TaosConnection connection,
            string commandText)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                return command.ExecuteNonQuery();
            }
        }

        public static T ExecuteScalar<T>(
            this TaosConnection connection,
            string commandText)
            => (T)connection.ExecuteScalar(commandText);

        private static object ExecuteScalar(this TaosConnection connection, string commandText)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;

                return command.ExecuteScalar();
            }
        }
    }
}
