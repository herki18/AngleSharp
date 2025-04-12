namespace SpellSystem;

#pragma warning disable CS8600
public class World
{
    public List<Entity> Entities = new List<Entity>();
    public Player? Player { get; private set; }
    public bool IsRunning { get; set; } = true;
    public float WorldTime { get; private set; } = 0f;
    public Random Random { get; } = new Random();

    private List<Entity> entitiesToAdd = new List<Entity>();
    private List<Entity> entitiesToRemove = new List<Entity>();

    public void AddEntity(Entity entity)
    {
        entitiesToAdd.Add(entity);
    }

    public void RemoveEntity(Entity entity)
    {
        entitiesToRemove.Add(entity);
    }

    public void SetPlayer(Player player)
    {
        Player = player;
        if (!Entities.Contains(player))
        {
            AddEntity(player);
        }
    }

    public void Update(float deltaTime)
    {
        WorldTime += deltaTime;

        // Add pending entities
        foreach (var entity in entitiesToAdd)
        {
            Entities.Add(entity);
        }
        entitiesToAdd.Clear();

        // Update all entities
        foreach (var entity in Entities)
        {
            entity.Update(deltaTime);
        }

        // Remove dead or pending removal entities
        foreach (var entity in entitiesToRemove)
        {
            Entities.Remove(entity);
        }
        entitiesToRemove.Clear();

        // Also remove any non-player entities that are dead
        Entities.RemoveAll(e => e != Player && e.Type != EntityType.Player && !e.IsAlive);
    }

    public void SpawnEnemyRandomly(float maxDistance = 15f)
    {
        if (Player == null) return;

        // Generate a random position around the player
        float angle = (float)(Random.NextDouble() * Math.PI * 2);
        float distance = (float)(Random.NextDouble() * maxDistance + 5f); // At least 5 units away

        Vector3 position = new Vector3(
            Player.Position.x + (float)Math.Cos(angle) * distance,
            0,
            Player.Position.z + (float)Math.Sin(angle) * distance
        );

        // Choose a random enemy type
        string[] enemyTypes = { "Goblin", "Orc", "Skeleton", "Wolf", "Bandit" };
        string enemyName = enemyTypes[Random.Next(enemyTypes.Length)];

        Entity enemy = new Entity(enemyName, position);
        enemy.Health = 50f + (float)Random.NextDouble() * 50f;
        enemy.MaxHealth = enemy.Health;
        enemy.MovementSpeed = 2f + (float)Random.NextDouble() * 3f;

        // Random resistances
        enemy.ElementalResistances["Fire"] = (float)Random.NextDouble() * 0.5f;
        enemy.ElementalResistances["Ice"] = (float)Random.NextDouble() * 0.5f;
        enemy.ElementalResistances["Lightning"] = (float)Random.NextDouble() * 0.5f;

        AddEntity(enemy);
        Console.WriteLine($"A {enemyName} appeared at {position}!");
    }

    public List<Entity> FindEntitiesInRadius(Vector3 center, float radius)
    {
        List<Entity> results = new List<Entity>();
        foreach (var e in Entities)
        {
            if (Vector3.Distance(e.Position, center) <= radius)
            {
                results.Add(e);
            }
        }
        return results;
    }

    public Entity? FindClosestEntity(Vector3 position, float maxDistance, Predicate<Entity>? filter = null)
    {
        Entity? closest = null;
        float closestDistance = maxDistance;

        foreach (var entity in Entities)
        {
            if (filter != null && !filter(entity)) continue;

            float distance = Vector3.Distance(position, entity.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = entity;
            }
        }

        return closest;
    }

    public void PrintWorldState()
    {
        Console.WriteLine("\n----- WORLD STATE -----");
        Console.WriteLine($"Time: {WorldTime:F1}s");
        Console.WriteLine($"Entities: {Entities.Count}");

        if (Player != null)
        {
            Console.WriteLine($"\n{Player.Name}: HP {Player.Health:F1}/{Player.MaxHealth} | " +
                              $"Mana {Player.Mana:F1}/{Player.MaxMana} | " +
                              $"Position {Player.Position}");

            if (Player.IsCasting && Player.CurrentlyCastingSpell != null)
            {
                float progress = Player.CastingProgress * 100f;
                Console.WriteLine($"CASTING: {Player.CurrentlyCastingSpell.Name} - {progress:F0}% complete");
                DrawCastingBar(20, progress / 100f);
            }
            else if (Player.CurrentSpell != null)
            {
                float manaCost = Player.CurrentSpell.UseNodeCosts
                    ? Player.CurrentSpell.RootNode.CalculateTotalManaCost()
                    : Player.CurrentSpell.ManaCost;

                Console.WriteLine($"Selected Spell: {Player.CurrentSpell.Name} " +
                                  $"(Mana: {manaCost:F1}, Cast Time: {Player.CurrentSpell.CastTime:F1}s)");
                Console.WriteLine($"Description: {Player.CurrentSpell.Description}");
            }
        }

        // Display enemies
        Console.WriteLine("\nEnemies:");
        int enemyCount = 0;
        foreach (var entity in Entities)
        {
            if (entity.Type == EntityType.Enemy && entity.IsAlive)
            {
                Console.WriteLine($"  {entity.Name}: HP {entity.Health:F1}/{entity.MaxHealth} | Position {entity.Position}");
                enemyCount++;
            }
        }

        if (enemyCount == 0)
        {
            Console.WriteLine("  No enemies nearby.");
        }

        Console.WriteLine("----------------------\n");
    }

    private void DrawCastingBar(int width, float fillPercentage)
    {
        Console.Write("[");
        int filledWidth = (int)(width * fillPercentage);

        for (int i = 0; i < width; i++)
        {
            if (i < filledWidth)
                Console.Write("█");
            else
                Console.Write("░");
        }

        Console.WriteLine("]");
    }
}