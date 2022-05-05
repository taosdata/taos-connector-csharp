﻿using System;
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

        private volatile bool _isDisposed;
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
            if (_isDisposed)
            {
                ThrowObjectDisposedException();
            }

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

            void ThrowObjectDisposedException()
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        /// <inheritdoc />
        public override void Return(T obj)
        {
            // When the pool is disposed or the obj is not returned to the pool, dispose it
            if (_isDisposed || !ReturnCore(obj))
            {
                DisposeItem(obj);
            }
        }

        private bool ReturnCore(T obj)
        {
            bool returnedTooPool = false;

            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem == null && Interlocked.CompareExchange(ref _firstItem, obj, null) == null)
                {
                    returnedTooPool = true;
                }
                else
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && !(returnedTooPool = Interlocked.CompareExchange(ref items[i].Element, obj, null) == null); i++)
                    {
                    }
                }
            }

            return returnedTooPool;
        }

        // PERF: the struct wrapper avoids array-covariance-checks from the runtime when assigning to elements of the array.
        [DebuggerDisplay("{Element}")]
        private protected struct ObjectWrapper
        {
            public T? Element;
        }

        public void Dispose()
        {
            _isDisposed = true;

            DisposeItem(_firstItem);
            _firstItem = null;

            ObjectWrapper[] items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                DisposeItem(items[i].Element);
                items[i].Element = null;
            }
        }

        private void DisposeItem(T item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
