namespace SpellSystem;

#pragma warning disable CS8600
public class SpellContext
{
    public Vector3? ImpactPoint { get; set; }
    public Entity? Caster { get; set; }
    public Vector3 CasterPosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public World? World { get; set; }
    public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();

    public T? GetSharedData<T>(string key, T? defaultValue = default) where T : class
    {
        if (SharedData.TryGetValue(key, out object? value) && value is T tValue)
        {
            return tValue;
        }
        return defaultValue;
    }

    public void SetSharedData<T>(string key, T value)
    {
        SharedData[key] = value!;
    }
}