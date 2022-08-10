using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IoTSharp.Data.Taos
{
    public class ConcurrentTaosQueue
    {
        public ConcurrentQueue<IntPtr> TaosQueue { get; }

        public ConcurrentTaosQueue(List<IntPtr> clients)
        {
            TaosQueue = new ConcurrentQueue<IntPtr>(clients);
        }

        public ConcurrentTaosQueue()
        {
            TaosQueue = new ConcurrentQueue<IntPtr>();
        }

        public void Return(IntPtr client)
        {
            Monitor.Enter(TaosQueue);
            TaosQueue.Enqueue(client);
            System.Diagnostics.Debug.WriteLine($"TaosQueue Return:{client}");
            Monitor.Pulse(TaosQueue);
            Monitor.Exit(TaosQueue);
           
        }
        int _ref = 0;
        public void AddRef()
        {
            lock (this)
            {
                _ref++;
            }
        }
        public int GetRef()
        {
            return _ref;
        }
        public void RemoveRef()
        {
            lock (this)
            {
                _ref--;
            }
        }
        public IntPtr Take()
        {
            Monitor.Enter(TaosQueue);
            if (TaosQueue.IsEmpty)
            {
                Monitor.Wait(TaosQueue);
            }
            TaosQueue.TryDequeue(out var client);
            System.Diagnostics.Debug.WriteLine($"TaosQueue Take:{client}");
            Monitor.Exit(TaosQueue);
            return client;
        }
    }
}
