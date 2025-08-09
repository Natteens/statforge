using System.Collections.Generic;
using System.Text;

namespace StatForge
{
    public static class StatIdPool
    {
        private static readonly Queue<string> recycledIds = new(1024);
        private static readonly StringBuilder stringBuilder = new(64);
        private static int nextId = 1;
        
        public static string GetId()
        {
            if (recycledIds.Count > 0)
                return recycledIds.Dequeue();
                
            stringBuilder.Clear();
            stringBuilder.Append("STAT_");
            stringBuilder.Append(nextId++);
            return stringBuilder.ToString();
        }
        
        public static void ReturnId(string id)
        {
            if (recycledIds.Count < 1024) 
                recycledIds.Enqueue(id);
        }
        
        public static void ClearPool()
        {
            recycledIds.Clear();
            nextId = 1;
        }
    }
}