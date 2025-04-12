namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public class ChainLightningNode : SpellNode
{
    public ChainLightningNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Delivery;
        SetParam("jumps", 3);
        SetParam("damage", 15.0f);
        SetParam("jumpRadius", 8.0f);
        SetParam("elementType", "Lightning");
        ManaCost = 15f; // Base mana cost for chain lightning, scales with jumps
    }

    public override bool CanExecute(SpellContext context)
    {
        return context.ImpactPoint.HasValue || context.GetSharedData<Entity>("HitEntity") != null;
    }

    public override void Execute(SpellContext context)
    {
        if (context.World == null) return;

        int jumps = GetParam<int>("jumps");
        float damage = GetParam<float>("damage");
        float jumpRadius = GetParam<float>("jumpRadius");
        string elementType = GetParam<string>("elementType");

        // Get initial target
        Entity? currentTarget = context.GetSharedData<Entity>("HitEntity");

        if (currentTarget == null && context.ImpactPoint.HasValue)
        {
            List<Entity> nearbyEntities = context.World.FindEntitiesInRadius(context.ImpactPoint.Value, 3.0f);
            currentTarget = nearbyEntities.FirstOrDefault(e => e != context.Caster && e.IsAlive);
        }

        if (currentTarget == null)
        {
            Console.WriteLine("[ChainLightningNode] No initial target found!");
            return;
        }

        // Apply damage to first target
        Console.WriteLine($"[ChainLightningNode] Lightning strikes {currentTarget.Name}!");
        currentTarget.TakeDamage(damage, elementType!);

        // Set of already hit entities to avoid loops
        HashSet<Entity> hitEntities = new HashSet<Entity> { currentTarget };

        // Chain to additional targets
        Vector3 lastPosition = currentTarget.Position;

        for (int i = 0; i < jumps; i++)
        {
            // Find next closest target not already hit
            Entity? nextTarget = context.World.FindClosestEntity(
                lastPosition,
                jumpRadius,
                e => e != context.Caster && e.IsAlive && !hitEntities.Contains(e));

            if (nextTarget == null)
            {
                Console.WriteLine($"[ChainLightningNode] Chain ended after {i} jumps - no more targets in range.");
                break;
            }

            // Reduce damage for each jump
            damage *= 0.7f;

            Console.WriteLine($"[ChainLightningNode] Lightning jumps to {nextTarget.Name} for {damage:F1} damage!");
            nextTarget.TakeDamage(damage, elementType!);

            hitEntities.Add(nextTarget);
            lastPosition = nextTarget.Position;

            // For visualization purposes in a real game
            Thread.Sleep(100);
        }

        // Execute child nodes
        ExecuteChildren(context);
    }
}