using System;

namespace StatForge
{
    public interface IStatModifier
    {
        string Id { get; }
        Stat TargetStat { get; }
        float Value { get; }
        ModifierType Type { get; }
        ModifierDuration Duration { get; }
        ModifierPriority Priority { get; }
        float RemainingTime { get; }
        bool IsExpired { get; }
        string Source { get; }
        object Tag { get; }
        
        bool Update(float deltaTime);
        bool ShouldRemove();
        void SetCondition(Func<bool> condition);
        IStatModifier Clone();
    }
}