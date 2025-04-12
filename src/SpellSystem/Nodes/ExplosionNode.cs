namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public class ExplosionNode : SpellNode
{
    public ExplosionNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Effect;
        SetParam("radius", 3.0f);
        SetParam("baseDamage", 20.0f);
        SetParam("elementType", "Fire");
        ManaCost = 8f; // Base mana cost for explosion effect
    }

    public override bool CanExecute(SpellContext context)
    {
        return context.ImpactPoint.HasValue;
    }

    public override void Execute(SpellContext context)
    {
        if (context.World == null || !context.ImpactPoint.HasValue) return;

        float radius = GetParam<float>("radius");
        float damage = GetParam<float>("baseDamage");
        string element = GetParam<string>("elementType");

        Vector3 center = context.ImpactPoint.Value;
        Console.WriteLine($"[ExplosionNode] BOOM! {element} explosion at {center}, radius={radius:F1}, damage={damage:F1}.");

        List<Entity> victims = context.World.FindEntitiesInRadius(center, radius);
        foreach (var e in victims)
        {
            if (e != context.Caster && e.IsAlive)
            {
                e.TakeDamage(damage, element!);
            }
        }

        // Execute child nodes
        ExecuteChildren(context);
    }
}