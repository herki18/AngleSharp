namespace SpellSystem;

#pragma warning disable CS8600
public class Player : Entity
{
    public List<SpellTemplate> KnownSpells { get; } = new List<SpellTemplate>();
    public int CurrentSpellIndex { get; set; } = 0;
    public float Mana { get; set; } = 100f;
    public float MaxMana { get; set; } = 100f;
    public float ManaRegenRate { get; set; } = 5f;

    // Add a reference to the World
    private World? currentWorld;

    // Casting system
    public bool IsCasting { get; private set; } = false;
    public float CastTimer { get; private set; } = 0f;
    public SpellTemplate? CurrentlyCastingSpell { get; private set; } = null;
    public Vector3? CastTargetPosition { get; private set; } = null;
    public float CastingProgress => CurrentlyCastingSpell != null ?
        Math.Min(CastTimer / CurrentlyCastingSpell.CastTime, 1.0f) : 0f;

    public Player(string name, Vector3 pos) : base(name, pos, EntityType.Player)
    {
        MaxHealth = 150f;
        Health = MaxHealth;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Regenerate mana
        Mana = Math.Min(Mana + ManaRegenRate * deltaTime, MaxMana);

        // Update casting
        if (IsCasting)
        {
            CastTimer += deltaTime;

            // Check if casting is complete
            if (CurrentlyCastingSpell != null && CastTimer >= CurrentlyCastingSpell.CastTime)
            {
                CompleteCasting();
            }
        }
    }

    public void BeginCastSpell(int spellIndex, World? world, Vector3? targetPosition = null)
    {
        if (world == null)
        {
            Console.WriteLine("Cannot cast spell: World is null.");
            return;
        }

        // Store the reference to the world
        currentWorld = world;

        if (IsCasting)
        {
            Console.WriteLine("Already casting a spell!");
            return;
        }

        if (spellIndex < 0 || spellIndex >= KnownSpells.Count)
        {
            Console.WriteLine("Invalid spell index.");
            return;
        }

        SpellTemplate spell = KnownSpells[spellIndex];

        // Calculate actual mana cost from the node tree if use_node_costs is enabled
        float manaCost = spell.UseNodeCosts ? spell.RootNode.CalculateTotalManaCost() : spell.ManaCost;

        if (Mana < manaCost)
        {
            Console.WriteLine($"Not enough mana to cast {spell.Name}. Required: {manaCost:F1}, Current: {Mana:F1}");
            return;
        }

        // Set target position or use direction from player
        Vector3 target = targetPosition ?? (Position + Direction * 10f);

        // Begin casting
        IsCasting = true;
        CastTimer = 0f;
        CurrentlyCastingSpell = spell;
        CastTargetPosition = target;

        Console.WriteLine($"{Name} begins casting {spell.Name}... (Cast time: {spell.CastTime:F1}s)");
    }

    private void CompleteCasting()
    {
        if (!IsCasting || CurrentlyCastingSpell == null) return;

        // Create spell context
        SpellContext context = new SpellContext();
        context.World = currentWorld; // Use the stored world reference
        context.Caster = this;
        context.CasterPosition = Position;
        context.TargetPosition = CastTargetPosition ?? (Position + Direction * 10f);

        // Calculate mana cost
        float manaCost = CurrentlyCastingSpell.UseNodeCosts ?
            CurrentlyCastingSpell.RootNode.CalculateTotalManaCost() :
            CurrentlyCastingSpell.ManaCost;

        // Check if we still have enough mana (it could have been used for something else)
        if (Mana < manaCost)
        {
            Console.WriteLine($"Not enough mana to complete casting {CurrentlyCastingSpell.Name}.");
            CancelCasting();
            return;
        }

        // Execute the spell
        if (CurrentlyCastingSpell.RootNode.CanExecute(context))
        {
            Mana -= manaCost;
            Console.WriteLine($"{Name} casts {CurrentlyCastingSpell.Name}! Cost: {manaCost:F1} mana. Remaining: {Mana:F1}/{MaxMana}");
            CurrentlyCastingSpell.RootNode.Execute(context);
        }

        // Reset casting state
        IsCasting = false;
        CurrentlyCastingSpell = null;
        CastTargetPosition = null;
    }

    public void CancelCasting()
    {
        if (IsCasting && CurrentlyCastingSpell != null)
        {
            Console.WriteLine($"{Name} cancels casting {CurrentlyCastingSpell.Name}.");
            IsCasting = false;
            CurrentlyCastingSpell = null;
            CastTargetPosition = null;
        }
    }

    public void CycleSpell(bool forward = true)
    {
        if (IsCasting)
        {
            Console.WriteLine("Can't change spells while casting!");
            return;
        }

        if (KnownSpells.Count == 0) return;

        if (forward)
        {
            CurrentSpellIndex = (CurrentSpellIndex + 1) % KnownSpells.Count;
        }
        else
        {
            CurrentSpellIndex = (CurrentSpellIndex - 1 + KnownSpells.Count) % KnownSpells.Count;
        }

        Console.WriteLine($"Selected spell: {KnownSpells[CurrentSpellIndex].Name}");
    }

    public SpellTemplate? CurrentSpell => KnownSpells.Count > 0 ? KnownSpells[CurrentSpellIndex] : null;
}