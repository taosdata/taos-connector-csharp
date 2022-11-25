using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
