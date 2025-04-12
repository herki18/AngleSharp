namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public class ProjectileNode : SpellNode
{
    public ProjectileNode(int id)
    {
        NodeID = id;
        NodeType = NodeType.Delivery;
        SetParam("speed", 10f);
        SetParam("range", 30f);
        SetParam("projectileName", "Projectile");
        SetParam("collisionRadius", 0.5f);
        ManaCost = 5f; // Base mana cost for projectile delivery
    }

    public override bool CanExecute(SpellContext context)
    {
        return context.Caster != null;
    }

    public override void Execute(SpellContext context)
    {
        string projName = GetParam<string>("projectileName");
        float maxRange = GetParam<float>("range");
        float speed = GetParam<float>("speed");
        float collisionRadius = GetParam<float>("collisionRadius");

        // Get direction from caster to target
        Vector3 position = context.CasterPosition;
        Vector3 direction = (context.TargetPosition - context.CasterPosition).Normalized();

        Console.WriteLine($"[ProjectileNode] Launching {projName} from {position} with speed {speed}.");

        // Simulate projectile flight.
        float deltaTime = 0.02f; // time step (e.g., 20ms per frame)
        float traveledDistance = 0f;
        bool hitDetected = false;

        while (traveledDistance < maxRange && context.World != null)
        {
            // Update projectile position.
            Vector3 step = direction * speed * deltaTime;
            position += step;
            traveledDistance += step.Magnitude();

            // Check for collision with any entities.
            List<Entity> nearby = context.World.FindEntitiesInRadius(position, collisionRadius);
            Entity? hitEntity = nearby.FirstOrDefault(e => e != context.Caster && e.IsAlive);

            if (hitEntity != null)
            {
                hitDetected = true;
                context.ImpactPoint = position;
                context.SetSharedData("HitEntity", hitEntity);
                Console.WriteLine($"[ProjectileNode] {projName} hit {hitEntity.Name} at {position} after traveling {traveledDistance:F1} units.");
                break;
            }
        }

        if (!hitDetected)
        {
            // If no collision, impact at the last position.
            context.ImpactPoint = position;
            Console.WriteLine($"[ProjectileNode] {projName} reached max range and impacted at {position}.");
        }

        // Execute child nodes
        ExecuteChildren(context);
    }
}