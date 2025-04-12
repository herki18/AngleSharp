namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public class HealingNode : SpellNode
{
    public HealingNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Effect;
        SetParam("healAmount", 20.0f);
        SetParam("radius", 5.0f);
        SetParam("affectSelf", true);
        SetParam("affectAllies", true);
        SetParam("healingType", HealingType.AreaEffect);
        ManaCost = 10f; // Base mana cost for healing
    }

    public override bool CanExecute(SpellContext context)
    {
        return context.Caster != null;
    }

    public override void Execute(SpellContext context)
    {
        if (context.World == null || context.Caster == null) return;

        float healAmount = GetParam<float>("healAmount");
        float radius = GetParam<float>("radius");
        bool affectSelf = GetParam<bool>("affectSelf");
        bool affectAllies = GetParam<bool>("affectAllies");
        HealingType healingType = GetParam<HealingType>("healingType");

        switch (healingType)
        {
            case HealingType.Touch:
                PerformTouchHealing(context, healAmount, affectSelf);
                break;

            case HealingType.Targeted:
                PerformTargetedHealing(context, healAmount);
                break;

            case HealingType.AreaEffect:
            default:
                PerformAreaHealing(context, healAmount, radius, affectSelf, affectAllies);
                break;
        }

        // Execute child nodes
        ExecuteChildren(context);
    }

    private void PerformTouchHealing(SpellContext context, float healAmount, bool affectSelf)
    {
        if (context.World == null || context.Caster == null) return;

        // Touch healing can only affect the caster or a direct touch target
        if (affectSelf)
        {
            Console.WriteLine($"[HealingNode] Touch healing caster for {healAmount:F1}");
            context.Caster.Heal(healAmount);
            return;
        }

        // Try to find an entity directly in front of the caster (touch range)
        Vector3 touchPosition = context.CasterPosition + context.Caster.Direction * 1.5f;
        Entity? touchTarget = context.World.FindClosestEntity(touchPosition, 1.0f,
            e => e != context.Caster && e.IsAlive && (e.Type == EntityType.Player || e.Type == EntityType.NPC));

        if (touchTarget != null)
        {
            Console.WriteLine($"[HealingNode] Touch healing {touchTarget.Name} for {healAmount:F1}");
            touchTarget.Heal(healAmount);
        }
        else
        {
            Console.WriteLine("[HealingNode] No touch target found in range");
        }
    }

    private void PerformTargetedHealing(SpellContext context, float healAmount)
    {
        if (context.World == null) return;

        // First check if we have a target in shared data
        Entity? target = context.GetSharedData<Entity>("HitEntity");

        // If not, check the impact point
        if (target == null && context.ImpactPoint.HasValue)
        {
            target = context.World.FindClosestEntity(context.ImpactPoint.Value, 2.0f,
                e => e.IsAlive && (e.Type == EntityType.Player || e.Type == EntityType.NPC));
        }

        if (target != null)
        {
            Console.WriteLine($"[HealingNode] Targeted healing on {target.Name} for {healAmount:F1}");
            target.Heal(healAmount);
        }
        else
        {
            Console.WriteLine("[HealingNode] No valid healing target found");
        }
    }

    private void PerformAreaHealing(SpellContext context, float healAmount, float radius, bool affectSelf, bool affectAllies)
    {
        if (context.World == null || context.Caster == null) return;

        Vector3 center = context.ImpactPoint ?? context.CasterPosition;

        Console.WriteLine($"[HealingNode] Area healing for {healAmount:F1} at {center}, radius={radius:F1}");

        List<Entity> targets = context.World.FindEntitiesInRadius(center, radius);

        foreach (var target in targets)
        {
            bool shouldHeal = false;

            if (target == context.Caster && affectSelf)
            {
                shouldHeal = true;
            }
            else if ((target.Type == EntityType.Player || target.Type == EntityType.NPC) && affectAllies)
            {
                shouldHeal = true;
            }

            if (shouldHeal && target.IsAlive)
            {
                target.Heal(healAmount);
            }
        }
    }
}