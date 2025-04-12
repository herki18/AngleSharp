namespace SpellSystem;

using Effects;

#pragma warning disable CS8600
public class Entity
{
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; } = Vector3.Forward;
    public bool IsAlive => Health > 0;
    public float MovementSpeed { get; set; } = 5f;
    public Dictionary<string, float> ElementalResistances { get; } = new Dictionary<string, float>();
    public List<StatusEffect> StatusEffects { get; } = new List<StatusEffect>();
    public float CooldownTimer { get; set; } = 0f;

    public Entity(string name, Vector3 pos, EntityType type = EntityType.Enemy)
    {
        Name = name;
        Position = pos;
        Type = type;

        // Default resistances
        ElementalResistances["Fire"] = 0f;
        ElementalResistances["Ice"] = 0f;
        ElementalResistances["Lightning"] = 0f;
        ElementalResistances["Poison"] = 0f;
    }

    public virtual void Update(float deltaTime)
    {
        // Update cooldown timer
        if (CooldownTimer > 0)
        {
            CooldownTimer -= deltaTime;
        }

        // Update status effects
        for (int i = StatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffects[i].Update(deltaTime);
            if (StatusEffects[i].IsExpired)
            {
                StatusEffects[i].OnRemove(this);
                StatusEffects.RemoveAt(i);
            }
        }
    }

    public void TakeDamage(float amount, string elementType = "Physical")
    {
        // Apply elemental resistance if applicable
        if (ElementalResistances.TryGetValue(elementType, out float resistance))
        {
            amount *= (1f - resistance);
        }

        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            OnDeath();
        }

        Console.WriteLine($"{Name} took {amount:F1} {elementType} damage. Remaining health: {Health:F1}/{MaxHealth}");
    }

    public void Heal(float amount)
    {
        Health = Math.Min(Health + amount, MaxHealth);
        Console.WriteLine($"{Name} healed for {amount:F1}. Health: {Health:F1}/{MaxHealth}");
    }

    public void AddStatusEffect(StatusEffect effect)
    {
        // Check if already has this type of effect
        StatusEffect? existingEffect = StatusEffects.FirstOrDefault(e => e.EffectType == effect.EffectType);
        if (existingEffect != null)
        {
            // Either refresh duration or stack effect
            existingEffect.Refresh(effect.Duration);
            Console.WriteLine($"{Name}'s {effect.EffectType} effect refreshed for {effect.Duration:F1} seconds.");
        }
        else
        {
            StatusEffects.Add(effect);
            effect.OnApply(this);
            Console.WriteLine($"{Name} affected by {effect.EffectType} for {effect.Duration:F1} seconds.");
        }
    }

    protected virtual void OnDeath()
    {
        Console.WriteLine($"{Name} has been defeated!");
    }

    public void Move(Vector3 direction, float deltaTime)
    {
        Vector3 normalizedDir = direction.Normalized();
        Position += normalizedDir * MovementSpeed * deltaTime;
    }

    public override string ToString()
    {
        return $"{Name} ({Type}) HP: {Health:F1}/{MaxHealth} at {Position}";
    }
}