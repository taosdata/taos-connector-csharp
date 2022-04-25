using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace IoTSharp.Data.Taos
{
    internal class TaosObjectPoolBase<T> : ObjectPool<T> where T : class, IDisposable
    {
        private protected readonly ObjectWrapper[] _items;
        private protected readonly IPooledObjectPolicy<T> _policy;
        private protected readonly bool _isDefaultPolicy;
        private protected T? _firstItem;

        // This class was introduced in 2.1 to avoid the interface call where possible
        private protected readonly PooledObjectPolicy<T>? _fastPolicy;

        /// <summary>
        /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
        /// </summary>
        /// <param name="policy">The pooling policy to use.</param>
        public TaosObjectPoolBase(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
        /// </summary>
        /// <param name="policy">The pooling policy to use.</param>
        /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
        public TaosObjectPoolBase(IPooledObjectPolicy<T> policy, int maximumRetained)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _fastPolicy = policy as PooledObjectPolicy<T>;
            _isDefaultPolicy = IsDefaultPolicy();

            // -1 due to _firstItem
            _items = new ObjectWrapper[maximumRetained - 1];

            bool IsDefaultPolicy()
            {
                var type = policy.GetType();

                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultPooledObjectPolicy<>);
            }
        }

        /// <inheritdoc />
        public override T Get()
        {
            var item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (var i = 0; i < items.Length; i++)
                {
                    item = items[i].Element;
                    if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    {
                        return item;
                    }
                }

                item = Create();
            }

            return item;
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        /// <inheritdoc />
        public override void Return(T obj)
        {
            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
                    {
                        obj.Dispose();
                    }
                }
            }
            else
            {
                obj.Dispose();
            }

        }

        // PERF: the struct wrapper avoids array-covariance-checks from the runtime when assigning to elements of the array.
        [DebuggerDisplay("{Element}")]
        private protected struct ObjectWrapper
        {
            public T? Element;
        }
    }
}
