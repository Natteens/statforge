using System.Collections.Generic;

namespace StatForge
{
    public static class ModifierPool
    {
        private static readonly Queue<IStatModifier> pool = new(256);
        private static int totalCreated;
        
        public static IStatModifier Get()
        {
            if (pool.Count > 0)
                return pool.Dequeue();
                
            totalCreated++;
            return new PooledStatModifier();
        }
        
        public static void Return(IStatModifier modifier)
        {
            if (modifier is PooledStatModifier pooled)
            {
                pooled.Reset();
                if (pool.Count < 256) 
                    pool.Enqueue(pooled);
            }
        }
        
        public static void ClearPool()
        {
            pool.Clear();
        }
        
        public static int GetStats() => totalCreated;
    }
}