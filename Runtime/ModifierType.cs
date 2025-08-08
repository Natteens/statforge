namespace StatForge
{
    public enum ModifierType
    {
        Additive,     // +10
        Subtractive,  // -5
        Multiplicative, // *1.5
        Override,     // =100 (substitui tudo)
        Percentage    // +50% do valor base
    }
    
    public enum ModifierDuration
    {
        Permanent,    // Permanente até ser removido
        Temporary,    // Duração por tempo
        Conditional   // Duração por condição customizada
    }
    
    public enum ModifierPriority
    {
        VeryLow = 0,
        Low = 100,
        Normal = 200,
        High = 300,
        VeryHigh = 400,
        Override = 1000
    }
}