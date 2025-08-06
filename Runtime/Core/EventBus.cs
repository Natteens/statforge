using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace StatForge.Core
{
    /// <summary>
    /// High-performance event bus with pooling for zero allocations
    /// </summary>
    public static class EventBus
    {
        private static readonly ConcurrentDictionary<Type, IEventHandlerCollection> handlers = 
            new ConcurrentDictionary<Type, IEventHandlerCollection>();
        private static readonly ObjectPool<EventData> eventPool = new ObjectPool<EventData>();
        
        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var collection = handlers.GetOrAdd(typeof(T), _ => new EventHandlerCollection<T>());
            ((EventHandlerCollection<T>)collection).Subscribe(handler);
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (handlers.TryGetValue(typeof(T), out var collection))
            {
                ((EventHandlerCollection<T>)collection).Unsubscribe(handler);
            }
        }
        
        /// <summary>
        /// Publish an event (zero allocation)
        /// </summary>
        public static void Publish<T>(T eventData) where T : IEvent
        {
            if (handlers.TryGetValue(typeof(T), out var collection))
            {
                ((EventHandlerCollection<T>)collection).Handle(eventData);
            }
        }
        
        /// <summary>
        /// Get a pooled event object
        /// </summary>
        public static EventData GetPooledEvent()
        {
            return eventPool.Get();
        }
        
        /// <summary>
        /// Return event to pool
        /// </summary>
        public static void ReturnToPool(EventData eventData)
        {
            eventData.Reset();
            eventPool.Return(eventData);
        }
        
        /// <summary>
        /// Clear all handlers
        /// </summary>
        public static void Clear()
        {
            handlers.Clear();
        }
    }
    
    /// <summary>
    /// Base interface for events
    /// </summary>
    public interface IEvent
    {
    }
    
    /// <summary>
    /// Pooled event data container
    /// </summary>
    public class EventData : IEvent
    {
        public string AttributeName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public float Timestamp { get; set; }
        public object Source { get; set; }
        
        public void Reset()
        {
            AttributeName = null;
            OldValue = null;
            NewValue = null;
            Timestamp = 0f;
            Source = null;
        }
    }
    
    /// <summary>
    /// Attribute change event
    /// </summary>
    public struct AttributeChangedEvent : IEvent
    {
        public string AttributeName;
        public object OldValue;
        public object NewValue;
        public object Source;
    }
    
    /// <summary>
    /// Modifier added event
    /// </summary>
    public struct ModifierAddedEvent : IEvent
    {
        public string AttributeName;
        public object ModifierValue;
        public float Duration;
        public object Source;
    }
    
    /// <summary>
    /// Interface for event handler collections
    /// </summary>
    internal interface IEventHandlerCollection
    {
    }
    
    /// <summary>
    /// Thread-safe event handler collection
    /// </summary>
    internal class EventHandlerCollection<T> : IEventHandlerCollection where T : IEvent
    {
        private readonly List<Action<T>> handlers = new List<Action<T>>();
        private readonly object lockObject = new object();
        
        public void Subscribe(Action<T> handler)
        {
            lock (lockObject)
            {
                handlers.Add(handler);
            }
        }
        
        public void Unsubscribe(Action<T> handler)
        {
            lock (lockObject)
            {
                handlers.Remove(handler);
            }
        }
        
        public void Handle(T eventData)
        {
            Action<T>[] handlersSnapshot;
            lock (lockObject)
            {
                handlersSnapshot = handlers.ToArray();
            }
            
            foreach (var handler in handlersSnapshot)
            {
                try
                {
                    handler(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
    
    /// <summary>
    /// Generic object pool for zero allocation performance
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentQueue<T> objects = new ConcurrentQueue<T>();
        private readonly Func<T> objectGenerator;
        private readonly Action<T> resetAction;
        
        public ObjectPool(Func<T> objectGenerator = null, Action<T> resetAction = null)
        {
            this.objectGenerator = objectGenerator ?? (() => new T());
            this.resetAction = resetAction;
        }
        
        public T Get()
        {
            if (objects.TryDequeue(out T item))
            {
                return item;
            }
            
            return objectGenerator();
        }
        
        public void Return(T item)
        {
            if (item != null)
            {
                resetAction?.Invoke(item);
                objects.Enqueue(item);
            }
        }
        
        public int Count => objects.Count;
    }
}