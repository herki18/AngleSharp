namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public class MultiTargetNode : SpellNode
{
    public MultiTargetNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Utility;
        SetParam("numTargets", 3);
        SetParam("radius", 10.0f);
        SetParam("preferClosest", true);
        ManaCost = 12f; // Base mana cost for multi-targeting, scales with numTargets
    }

    public override bool CanExecute(SpellContext context)
    {
        return context.ImpactPoint.HasValue;
    }

    public override void Execute(SpellContext context)
    {
        if (context.World == null || !context.ImpactPoint.HasValue) return;

        int numTargets = GetParam<int>("numTargets");
        float radius = GetParam<float>("radius");
        bool preferClosest = GetParam<bool>("preferClosest");

        Vector3 center = context.ImpactPoint.Value;
        List<Entity> targets = context.World.FindEntitiesInRadius(center, radius)
            .Where(e => e != context.Caster && e.IsAlive)
            .ToList();

        if (preferClosest)
        {
            targets = targets.OrderBy(e => Vector3.Distance(center, e.Position)).ToList();
        }

        // Limit to the requested number of targets
        targets = targets.Take(numTargets).ToList();

        Console.WriteLine($"[MultiTargetNode] Found {targets.Count} targets within {radius:F1} units of {center}");

        // Store the original impact point
        Vector3 originalImpact = context.ImpactPoint.Value;

        // Execute child nodes for each target
        foreach (var target in targets)
        {
            context.ImpactPoint = target.Position;
            context.SetSharedData("HitEntity", target);

            foreach (var child in ChildNodes)
            {
                if (child.CanExecute(context))
                {
                    child.Execute(context);
                }
            }
        }

        // Restore the original impact point
        context.ImpactPoint = originalImpact;
    }
}