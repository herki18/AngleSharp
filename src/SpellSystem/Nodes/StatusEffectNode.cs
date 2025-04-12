namespace SpellSystem.Nodes;

using SpellSystem.Effects;

#pragma warning disable CS8600
public class StatusEffectNode : SpellNode
{
    public StatusEffectNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Effect;
        SetParam("effectType", "Burn");
        SetParam("duration", 5.0f);
        SetParam("strength", 5.0f);
        ManaCost = 5f; // Base mana cost for status effects
    }

    public override bool CanExecute(SpellContext context)
    {
        return true;
    }

    public override void Execute(SpellContext context)
    {
        if (context.World == null) return;

        string effectType = GetParam<string>("effectType");
        float duration = GetParam<float>("duration");
        float strength = GetParam<float>("strength");

        // Get hit entity either from shared data or from radius check
        Entity? target = context.GetSharedData<Entity>("HitEntity");

        if (target == null && context.ImpactPoint.HasValue)
        {
            float radius = GetParam<float>("radius", 3.0f);
            List<Entity> nearbyEntities = context.World.FindEntitiesInRadius(context.ImpactPoint.Value, radius);
            target = nearbyEntities.FirstOrDefault(e => e != context.Caster && e.IsAlive);
        }

        if (target != null && target.IsAlive)
        {
            StatusEffect? effect = null;

            switch (effectType!.ToLower())
            {
                case "burn":
                    effect = new BurnEffect(duration, strength);
                    break;
                case "freeze":
                    effect = new FreezeEffect(duration, strength / 100f); // Convert to percentage
                    break;
                default:
                    Console.WriteLine($"[StatusEffectNode] Unknown effect type: {effectType}");
                    break;
            }

            if (effect != null)
            {
                target.AddStatusEffect(effect);
                Console.WriteLine($"[StatusEffectNode] Applied {effectType} effect to {target.Name} for {duration:F1} seconds.");
            }
        }
        else
        {
            Console.WriteLine($"[StatusEffectNode] No target found to apply {effectType} effect.");
        }

        // Execute child nodes
        ExecuteChildren(context);
    }
}