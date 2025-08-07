using System;

namespace StatForge
{
    public static class StatEvents
    {
        public static class Global
        {
            public static event Action<Stat> OnAnyStatChanged;
            public static event Action<Stat, StatModifier> OnModifierAdded;
            public static event Action<Stat, StatModifier> OnModifierRemoved;
            public static event Action<Stat, float, float> OnValueChanged; // stat, oldValue, newValue
            
            internal static void NotifyStatChanged(Stat stat)
            {
                OnAnyStatChanged?.Invoke(stat);
            }
            
            internal static void NotifyModifierAdded(Stat stat, StatModifier modifier)
            {
                OnModifierAdded?.Invoke(stat, modifier);
            }
            
            internal static void NotifyModifierRemoved(Stat stat, StatModifier modifier)
            {
                OnModifierRemoved?.Invoke(stat, modifier);
            }
            
            internal static void NotifyValueChanged(Stat stat, float oldValue, float newValue)
            {
                OnValueChanged?.Invoke(stat, oldValue, newValue);
            }
        }
        
        public class StatEventArgs : EventArgs
        {
            public Stat Stat { get; }
            public float OldValue { get; }
            public float NewValue { get; }
            public StatModifier Modifier { get; }
            
            public StatEventArgs(Stat stat, float oldValue, float newValue)
            {
                Stat = stat;
                OldValue = oldValue;
                NewValue = newValue;
            }
            
            public StatEventArgs(Stat stat, StatModifier modifier)
            {
                Stat = stat;
                Modifier = modifier;
            }
        }
        
        public class StatEventHandler
        {
            private readonly Stat _stat;
            
            public event Action<float, float> OnValueChanged; // oldValue, newValue
            public event Action<StatModifier> OnModifierAdded;
            public event Action<StatModifier> OnModifierRemoved;
            public event Action OnStatDestroyed;
            
            internal StatEventHandler(Stat stat)
            {
                _stat = stat;
            }
            
            internal void NotifyValueChanged(float oldValue, float newValue)
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
            
            internal void NotifyModifierAdded(StatModifier modifier)
            {
                OnModifierAdded?.Invoke(modifier);
            }
            
            internal void NotifyModifierRemoved(StatModifier modifier)
            {
                OnModifierRemoved?.Invoke(modifier);
            }
            
            internal void NotifyDestroyed()
            {
                OnStatDestroyed?.Invoke();
            }
        }
    }
}