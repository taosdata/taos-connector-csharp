using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace IoTSharp.Data.Taos
{
    internal class TaosConnectorSource : TaosObjectPoolBase<TaosConnector>
    {
        public TaosConnectorSource(IPooledObjectPolicy<TaosConnector> policy) : base(policy)
        {
        }

        public TaosConnectorSource(IPooledObjectPolicy<TaosConnector> policy, int maximumRetained) : base(policy, maximumRetained)
        {
        }
    }
}
