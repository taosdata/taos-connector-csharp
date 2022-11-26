using System.Data;
using TDengineDriver;

namespace IoTSharp.Data.Taos.Protocols
{
    internal interface ITaosProtocol
    {
        void InitTaos(string configdir, int shell_activity_timer, string locale, string charset);

        public string GetServerVersion();

        public string GetClientVersion();

        nint Take();

        void Return(nint taos);

        bool Open(TaosConnectionStringBuilder connectionStringBuilder);

        void Close(TaosConnectionStringBuilder connectionStringBuilder);

        bool ChangeDatabase(string databaseName);

        TaosDataReader ExecuteReader(CommandBehavior behavior, TaosCommand command);

        int ExecuteBulkInsert(string[] lines, TDengineSchemalessProtocol protocol, TDengineSchemalessPrecision precision);
    }
}