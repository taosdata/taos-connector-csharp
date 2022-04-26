using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos
{
    internal class TaosConnector : IDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        internal TaosConnection TaosConnection { get; set; }

        public void Dispose()
        {
            TaosConnection?.Dispose();
        }
    }
}
