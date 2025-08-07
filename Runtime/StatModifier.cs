using System;
using UnityEngine;

namespace StatForge
{
    [Serializable]
    public class StatModifier
    {
        public enum ModifierType
        {
            Flat,           // +10 damage
            Percentage,     // +25% damage
            Override        // Set to specific value
        }
        
        [SerializeField] private float value;
        [SerializeField] private ModifierType type;
        [SerializeField] private float duration;
        [SerializeField] private string source;
        [SerializeField] private bool isPermanent;
        
        private float startTime;
        private bool isActive;
        
        public float Value => value;
        public ModifierType Type => type;
        public float Duration => duration;
        public string Source => source;
        public bool IsPermanent => isPermanent;
        public bool IsActive => isActive && (isPermanent || Time.time < startTime + duration);
        public float TimeRemaining => isPermanent ? float.MaxValue : Mathf.Max(0f, startTime + duration - Time.time);
        
        public event Action<StatModifier> OnExpired;
        
        public StatModifier(float value, ModifierType type = ModifierType.Flat, float duration = 0f, string source = "")
        {
            this.value = value;
            this.type = type;
            this.duration = duration;
            this.source = source;
            this.isPermanent = duration <= 0f;
            this.isActive = false;
        }
        
        public void Activate()
        {
            isActive = true;
            startTime = Time.time;
        }
        
        public void Deactivate()
        {
            isActive = false;
        }
        
        public void UpdateModifier()
        {
            if (isActive && !isPermanent && Time.time >= startTime + duration)
            {
                isActive = false;
                OnExpired?.Invoke(this);
            }
        }
        
        public float Apply(float baseValue)
        {
            if (!IsActive) return baseValue;
            
            switch (type)
            {
                case ModifierType.Flat:
                    return baseValue + value;
                case ModifierType.Percentage:
                    return baseValue * (1f + value / 100f);
                case ModifierType.Override:
                    return value;
                default:
                    return baseValue;
            }
        }
        
        public override string ToString()
        {
            var typeSymbol = type switch
            {
                ModifierType.Flat => value >= 0 ? "+" : "",
                ModifierType.Percentage => value >= 0 ? "+%" : "%",
                ModifierType.Override => "=",
                _ => ""
            };
            
            var durationText = isPermanent ? "∞" : $"{TimeRemaining:F1}s";
            var sourceText = string.IsNullOrEmpty(source) ? "" : $" ({source})";
            
            return $"{typeSymbol}{value} [{durationText}]{sourceText}";
        }
    }
}