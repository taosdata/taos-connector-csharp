using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace IoTSharp.Data.Taos
{
    internal class TaosPooledObjectPolicy : DefaultPooledObjectPolicy<TaosConnector>
    {
        public override TaosConnector Create()
        {
            Debug.Print("create TaosConnector");
            return new TaosConnector();
        }

        public override bool Return(TaosConnector obj)
        {
            if (obj.TaosConnection.State == ConnectionState.Closed || obj.TaosConnection.State == ConnectionState.Broken)
                return false;
            return true;
        }
    }
}
