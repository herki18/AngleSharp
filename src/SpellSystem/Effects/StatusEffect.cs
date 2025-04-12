namespace SpellSystem.Effects;

#pragma warning disable CS8600
public abstract class StatusEffect
{
    public string EffectType { get; protected set; } = string.Empty;
    public float Duration { get; protected set; }
    public float ElapsedTime { get; protected set; } = 0f;
    public bool IsExpired => ElapsedTime >= Duration;

    public StatusEffect(float duration)
    {
        Duration = duration;
    }

    public virtual void Update(float deltaTime)
    {
        ElapsedTime += deltaTime;
    }

    public virtual void OnApply(Entity target) { }
    public virtual void OnTick(Entity? target, float deltaTime) { }
    public virtual void OnRemove(Entity target) { }

    public void Refresh(float newDuration)
    {
        Duration = Math.Max(Duration - ElapsedTime, newDuration);
        ElapsedTime = 0f;
    }
}