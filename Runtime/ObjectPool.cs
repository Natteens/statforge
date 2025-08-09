using System;
using System.Collections.Generic;

namespace StatForge
{
    public class ObjectPool<T> where T : class
    {
        private readonly Queue<T> pool = new();
        private readonly Func<T> createFunc;
        private readonly Action<T> resetAction;
        private readonly int maxSize;

        public ObjectPool(Func<T> createFunc, Action<T> resetAction = null, int maxSize = 100)
        {
            this.createFunc = createFunc;
            this.resetAction = resetAction;
            this.maxSize = maxSize;
        }

        public T Get()
        {
            if (pool.Count > 0)
            {
                var item = pool.Dequeue();
                return item;
            }
            return createFunc();
        }

        public void Return(T item)
        {
            if (item == null || pool.Count >= maxSize) return;
            
            resetAction?.Invoke(item);
            pool.Enqueue(item);
        }

        public void Clear()
        {
            pool.Clear();
        }
    }
}