using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TDengineDriver;

namespace IoTSharp.Data.Taos
{
    static class TaosPoolManager
    {
        internal const int InitialPoolsSize = 10;

        static readonly object Lock = new();
        static volatile (string Key, TaosConnectorSource Pool)[] _pools = new (string, TaosConnectorSource)[InitialPoolsSize];
        static volatile int _nextSlot;

        internal static (string Key, TaosConnectorSource Pool)[] Pools => _pools;

        internal static bool TryGetValue(string key, [NotNullWhen(true)] out TaosConnectorSource? pool)
        {
            // Note that pools never get removed. _pools is strictly append-only.
            var nextSlot = _nextSlot;
            var pools = _pools;
            var sw = new SpinWait();

            // First scan the pools and do reference equality on the connection strings
            for (var i = 0; i < nextSlot; i++)
            {
                var cp = pools[i];
                if (ReferenceEquals(cp.Key, key))
                {
                    // It's possible that this pool entry is currently being written: the connection string
                    // component has already been written, but the pool component is just about to be. So we
                    // loop on the pool until it's non-null
                    while (Volatile.Read(ref cp.Pool) == null)
                        sw.SpinOnce();
                    pool = cp.Pool;
                    return true;
                }
            }

            // Next try value comparison on the strings
            for (var i = 0; i < nextSlot; i++)
            {
                var cp = pools[i];
                if (cp.Key == key)
                {
                    // See comment above
                    while (Volatile.Read(ref cp.Pool) == null)
                        sw.SpinOnce();
                    pool = cp.Pool;
                    return true;
                }
            }

            pool = null;
            return false;
        }

        internal static TaosConnectorSource GetOrAdd(string key, TaosConnectorSource pool)
        {
            lock (Lock)
            {
                if (TryGetValue(key, out var result))
                    return result;

                // May need to grow the array.
                if (_nextSlot == _pools.Length)
                {
                    var newPools = new (string, TaosConnectorSource)[_pools.Length * 2];
                    Array.Copy(_pools, newPools, _pools.Length);
                    _pools = newPools;
                }

                _pools[_nextSlot].Key = key;
                _pools[_nextSlot].Pool = pool;
                Interlocked.Increment(ref _nextSlot);
                return pool;
            }
        }

        internal static void ClearAll()
        {
            lock (Lock)
            {
                var pools = _pools;
                for (var i = 0; i < _nextSlot; i++)
                {
                    var cp = pools[i];
                    if (cp.Key == null)
                        return;
                    cp.Pool = null;
                }

                TDengine.Cleanup();
            }
        }

        static TaosPoolManager()
        {
            // When the appdomain gets unloaded (e.g. web app redeployment) attempt to nicely
            // close idle connectors to prevent errors in PostgreSQL logs (#491).
            AppDomain.CurrentDomain.DomainUnload += (sender, args) => ClearAll();
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => ClearAll();
        }
    }
}
