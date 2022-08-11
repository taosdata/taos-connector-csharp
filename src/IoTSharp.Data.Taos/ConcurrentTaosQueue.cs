using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
            Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 归还 {client}");
            Monitor.Pulse(TaosQueue);
            Monitor.Exit(TaosQueue);
            Thread.Sleep(0);
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
        public int Timeout { get; set; }
        public IntPtr Take()
        {
            IntPtr client = IntPtr.Zero;
            Monitor.Enter(TaosQueue);
            if (TaosQueue.IsEmpty)
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 连接池已空,请等待 超时时长:{Timeout}");
                Monitor.Wait(TaosQueue, TimeSpan.FromSeconds(Timeout));
            }
            if (!TaosQueue.TryDequeue(out client))
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 从连接池获取连接失败，等待并重试");
            }
            else
            {
                Debug.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId} 拿走 {client}");
            }
            Monitor.Exit(TaosQueue);
            if (client == IntPtr.Zero)
            {
                throw new TimeoutException($"Connection pool is empty and wait time out({Timeout}s)!");
            }
            return client;
        }
    }
}
