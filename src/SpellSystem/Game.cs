namespace SpellSystem;

using Nodes;

#pragma warning disable CS8600
public class Game
{
    private World world;
    private Player? player;
    private bool isRunning = true;
    private float enemySpawnTimer = 0f;
    private float enemySpawnInterval = 5f;
    private float updateInterval = 0.1f; // seconds per game update
    private float renderInterval = 1.0f; // seconds per render update
    private float updateTimer = 0f;
    private float renderTimer = 0f;

    public Game()
    {
        world = new World();
        SetupPlayer();
        SetupSpells();
    }

    private void SetupPlayer()
    {
        player = new Player("Wizard", Vector3.Zero);
        world.SetPlayer(player);
    }

    private void SetupSpells()
    {
        if (player == null) return;

        // Create various spells and add them to player's known spells

        // 1. Fireball
        ExplosionNode fireballExplosion = new ExplosionNode(2);
        fireballExplosion.SetParam("baseDamage", 25.0f);
        fireballExplosion.SetParam("radius", 4.0f);
        fireballExplosion.SetParam("elementType", "Fire");
        fireballExplosion.ManaCost = 8f;

        StatusEffectNode burnEffect = new StatusEffectNode(3);
        burnEffect.SetParam("effectType", "Burn");
        burnEffect.SetParam("duration", 3.0f);
        burnEffect.SetParam("strength", 8.0f);
        burnEffect.ManaCost = 5f;

        ProjectileNode fireballProjectile = new ProjectileNode(1);
        fireballProjectile.SetParam("projectileName", "Fireball");
        fireballProjectile.SetParam("speed", 15f);
        fireballProjectile.ManaCost = 7f;
        fireballProjectile.AddChild(fireballExplosion);
        fireballExplosion.AddChild(burnEffect);

        SpellTemplate fireball = new SpellTemplate(
            "Fireball",
            "Launches a ball of fire that explodes on impact, causing burn damage.",
            20f, // mana cost
            1.5f, // cast time
            fireballProjectile,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(fireball);

        // 2. Ice Spike
        StatusEffectNode freezeEffect = new StatusEffectNode(6);
        freezeEffect.SetParam("effectType", "Freeze");
        freezeEffect.SetParam("duration", 4.0f);
        freezeEffect.SetParam("strength", 50.0f); // 50% slow
        freezeEffect.ManaCost = 7f;

        ProjectileNode iceSpikeProjectile = new ProjectileNode(4);
        iceSpikeProjectile.SetParam("projectileName", "Ice Spike");
        iceSpikeProjectile.SetParam("speed", 25f);
        iceSpikeProjectile.ManaCost = 5f;

        ExplosionNode iceExplosion = new ExplosionNode(5);
        iceExplosion.SetParam("baseDamage", 15.0f);
        iceExplosion.SetParam("radius", 2.0f);
        iceExplosion.SetParam("elementType", "Ice");
        iceExplosion.ManaCost = 6f;

        iceSpikeProjectile.AddChild(iceExplosion);
        iceExplosion.AddChild(freezeEffect);

        SpellTemplate iceSpike = new SpellTemplate(
            "Ice Spike",
            "Launches a spike of ice that freezes enemies on impact.",
            15f, // mana cost
            1.0f, // cast time
            iceSpikeProjectile,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(iceSpike);

        // 3. Chain Lightning
        ChainLightningNode chainLightning = new ChainLightningNode(7);
        chainLightning.SetParam("jumps", 4);
        chainLightning.SetParam("damage", 20.0f);
        chainLightning.ManaCost = 15f;

        ProjectileNode lightningProjectile = new ProjectileNode(8);
        lightningProjectile.SetParam("projectileName", "Lightning Bolt");
        lightningProjectile.SetParam("speed", 40f);
        lightningProjectile.ManaCost = 8f;
        lightningProjectile.AddChild(chainLightning);

        SpellTemplate lightningBolt = new SpellTemplate(
            "Chain Lightning",
            "Fires a bolt of lightning that jumps between nearby enemies.",
            35f, // mana cost
            2.0f, // cast time
            lightningProjectile,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(lightningBolt);

        // 4. Touch Healing
        HealingNode touchHealing = new HealingNode(9);
        touchHealing.SetParam("healAmount", 40.0f);
        touchHealing.SetParam("healingType", HealingType.Touch);
        touchHealing.SetParam("affectSelf", true);
        touchHealing.ManaCost = 15f;

        SpellTemplate touchHealSpell = new SpellTemplate(
            "Healing Touch",
            "Powerful healing spell that requires direct contact with the target.",
            15f, // base mana cost
            1.0f, // cast time
            touchHealing,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(touchHealSpell);

        // 5. Healing Aura
        HealingNode areaHealing = new HealingNode(10);
        areaHealing.SetParam("healAmount", 25.0f);
        areaHealing.SetParam("radius", 5.0f);
        areaHealing.SetParam("healingType", HealingType.AreaEffect);
        areaHealing.SetParam("affectSelf", true);
        areaHealing.SetParam("affectAllies", true);
        areaHealing.ManaCost = 20f;

        SpellTemplate healingAura = new SpellTemplate(
            "Healing Aura",
            "Creates an aura that heals the caster and nearby allies.",
            20f, // mana cost
            3.0f, // cast time
            areaHealing,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(healingAura);

        // 6. Multi-fireball
        ExplosionNode multiFireballExplosion = new ExplosionNode(11);
        multiFireballExplosion.SetParam("baseDamage", 15.0f);
        multiFireballExplosion.SetParam("radius", 2.0f);
        multiFireballExplosion.SetParam("elementType", "Fire");
        multiFireballExplosion.ManaCost = 5f; // Reduced cost per explosion

        MultiTargetNode multiTarget = new MultiTargetNode(12);
        multiTarget.SetParam("numTargets", 3);
        multiTarget.SetParam("radius", 15.0f);
        multiTarget.AddChild(multiFireballExplosion);
        multiTarget.ManaCost = 12f;

        ProjectileNode multiFireballDelivery = new ProjectileNode(13);
        multiFireballDelivery.SetParam("projectileName", "Multi-Fireball");
        multiFireballDelivery.SetParam("speed", 10f);
        multiFireballDelivery.AddChild(multiTarget);
        multiFireballDelivery.ManaCost = 8f;

        SpellTemplate multiFireball = new SpellTemplate(
            "Multi-Fireball",
            "Launches a spell that splits into multiple fireballs targeting nearby enemies.",
            40f, // mana cost override if not using node costs
            3.5f, // cast time
            multiFireballDelivery,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(multiFireball);

        // 7. Targeted Healing Bolt
        ProjectileNode healingProjectile = new ProjectileNode(14);
        healingProjectile.SetParam("projectileName", "Healing Bolt");
        healingProjectile.SetParam("speed", 20f);
        healingProjectile.ManaCost = 8f;

        HealingNode targetedHealing = new HealingNode(15);
        targetedHealing.SetParam("healAmount", 35.0f);
        targetedHealing.SetParam("healingType", HealingType.Targeted);
        targetedHealing.ManaCost = 12f;

        healingProjectile.AddChild(targetedHealing);

        SpellTemplate healingBolt = new SpellTemplate(
            "Healing Bolt",
            "Fires a bolt of healing energy that can be targeted at a distance.",
            25f, // mana cost override
            2.0f, // cast time
            healingProjectile,
            true, // use node costs
            true  // can be interrupted
        );
        player.KnownSpells.Add(healingBolt);

        // 8. Instant Zap (no cast time)
        ExplosionNode zapExplosion = new ExplosionNode(16);
        zapExplosion.SetParam("baseDamage", 10.0f);
        zapExplosion.SetParam("radius", 1.5f);
        zapExplosion.SetParam("elementType", "Lightning");
        zapExplosion.ManaCost = 12f;

        SpellTemplate instantZap = new SpellTemplate(
            "Lightning Zap",
            "An instant lightning spell that deals minor damage but can be cast immediately.",
            12f, // mana cost
            0.0f, // cast time (instant)
            zapExplosion,
            true, // use node costs
            false // cannot be interrupted (instant)
        );
        player.KnownSpells.Add(instantZap);
    }

    public void Start()
    {
        Console.WriteLine("=== Magic Spell System Demo ===");
        Console.WriteLine("W/A/S/D: Move player");
        Console.WriteLine("1-5: Select spells");
        Console.WriteLine("Space: Cast selected spell");
        Console.WriteLine("Q: Quit");
        Console.WriteLine("\nGame started! Use the controls to interact.\n");

        world.PrintWorldState();

        GameLoop();
    }

    private void GameLoop()
    {
        DateTime lastUpdateTime = DateTime.Now;

        while (isRunning)
        {
            DateTime currentTime = DateTime.Now;
            float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;
            lastUpdateTime = currentTime;

            // Cap delta time to prevent jumps after long pauses
            deltaTime = Math.Min(deltaTime, 0.1f);

            // Update game state
            updateTimer += deltaTime;
            if (updateTimer >= updateInterval)
            {
                Update(updateInterval);
                updateTimer = 0f;
            }

            // Render game state
            renderTimer += deltaTime;
            if (renderTimer >= renderInterval)
            {
                Render();
                renderTimer = 0f;
            }

            // Process input
            ProcessInput();

            // Small delay to prevent CPU overuse
            Thread.Sleep(10);
        }

        Console.WriteLine("Game ended. Thanks for playing!");
    }

    private void Update(float deltaTime)
    {
        // Update the world
        world.Update(deltaTime);

        // Handle enemy spawning
        enemySpawnTimer += deltaTime;
        if (enemySpawnTimer >= enemySpawnInterval)
        {
            world.SpawnEnemyRandomly();
            enemySpawnTimer = 0f;

            // Gradually decrease spawn interval to increase difficulty
            enemySpawnInterval = Math.Max(2.0f, enemySpawnInterval * 0.98f);
        }

        // Game over check
        if (player != null && !player.IsAlive)
        {
            Console.WriteLine("\n--- GAME OVER ---");
            Console.WriteLine($"You survived for {world.WorldTime:F1} seconds!");
            Console.WriteLine("Press any key to exit...");
            isRunning = false;
        }
    }

    private void Render()
    {
        // Clear screen and display game state
        Console.Clear();
        world.PrintWorldState();
        PrintControls();
    }

    private void PrintControls()
    {
        Console.WriteLine("Controls: [W/A/S/D] Move | [1-5] Select Spell | [Space] Cast | [Q] Quit");
    }

    private void ProcessInput()
    {
        if (Console.KeyAvailable && player != null)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            // Movement
            Vector3 moveDirection = Vector3.Zero;

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                    moveDirection.z = 1;
                    break;
                case ConsoleKey.S:
                    moveDirection.z = -1;
                    break;
                case ConsoleKey.A:
                    moveDirection.x = -1;
                    break;
                case ConsoleKey.D:
                    moveDirection.x = 1;
                    break;

                // Spell selection
                case ConsoleKey.D1:
                case ConsoleKey.D2:
                case ConsoleKey.D3:
                case ConsoleKey.D4:
                case ConsoleKey.D5:
                case ConsoleKey.D6:
                case ConsoleKey.D7:
                case ConsoleKey.D8:
                case ConsoleKey.D9:
                    int spellIndex = keyInfo.Key - ConsoleKey.D1;
                    if (spellIndex >= 0 && spellIndex < player.KnownSpells.Count)
                    {
                        player.CurrentSpellIndex = spellIndex;
                        Console.WriteLine($"Selected spell: {player.KnownSpells[spellIndex].Name}");
                    }
                    break;

                // Spell casting
                case ConsoleKey.Spacebar:
                    if (player.IsCasting)
                    {
                        Console.WriteLine("Already casting a spell! Press 'C' to cancel.");
                        break;
                    }

                    // Find a target (closest enemy within range)
                    Entity? target = world.FindClosestEntity(
                        player.Position,
                        30f,
                        e => e.Type == EntityType.Enemy && e.IsAlive);

                    if (target != null)
                    {
                        player.BeginCastSpell(player.CurrentSpellIndex, world, target.Position);
                    }
                    else
                    {
                        // Cast in the direction the player is facing
                        Vector3 targetPos = player.Position + player.Direction * 10f;
                        player.BeginCastSpell(player.CurrentSpellIndex, world, targetPos);
                    }
                    break;

                // Cancel casting
                case ConsoleKey.C:
                    player.CancelCasting();
                    break;

                // Quit game
                case ConsoleKey.Q:
                    isRunning = false;
                    break;
            }

            // Apply movement - if moving, cancel casting if the spell can be interrupted
            if (!moveDirection.Equals(Vector3.Zero))
            {
                // If moving while casting, check if spell can be interrupted
                if (player.IsCasting && player.CurrentlyCastingSpell?.CanBeInterrupted == true)
                {
                    player.CancelCasting();
                    Console.WriteLine("Spell casting interrupted by movement.");
                }

                player.Direction = moveDirection.Normalized();
                player.Move(moveDirection, 0.1f);
            }
        }
    }
}