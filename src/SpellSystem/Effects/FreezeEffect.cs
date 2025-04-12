namespace SpellSystem.Effects;

#pragma warning disable CS8600
public class FreezeEffect : StatusEffect
{
    private float slowPercentage;

    public FreezeEffect(float duration, float slowPercentage) : base(duration)
    {
        EffectType = "Freeze";
        this.slowPercentage = slowPercentage;
    }

    public override void OnApply(Entity target)
    {
        target.MovementSpeed *= (1f - slowPercentage);
        Console.WriteLine($"{target.Name} is slowed by {slowPercentage * 100}%!");
    }

    public override void OnRemove(Entity target)
    {
        target.MovementSpeed /= (1f - slowPercentage);
        Console.WriteLine($"{target.Name} is no longer slowed.");
    }
}