namespace SpellSystem.Effects;

#pragma warning disable CS8600
public class BurnEffect : StatusEffect
{
    private float damagePerSecond;
    private float tickTimer = 0f;
    private const float TICK_RATE = 0.5f;
    private Entity? targetEntity;

    public BurnEffect(float duration, float damagePerSecond) : base(duration)
    {
        EffectType = "Burn";
        this.damagePerSecond = damagePerSecond;
    }

    public override void OnApply(Entity target)
    {
        targetEntity = target;
        Console.WriteLine($"{target.Name} is burning!");
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        tickTimer += deltaTime;

        if (tickTimer >= TICK_RATE && targetEntity != null)
        {
            tickTimer -= TICK_RATE;
            OnTick(targetEntity, TICK_RATE);
        }
    }

    public override void OnTick(Entity? target, float deltaTime)
    {
        if (target != null && target.IsAlive)
        {
            float damage = damagePerSecond * deltaTime;
            target.TakeDamage(damage, "Fire");
        }
    }

    public override void OnRemove(Entity target)
    {
        Console.WriteLine($"{target.Name} is no longer burning.");
        targetEntity = null;
    }
}