namespace SpellSystem;

using Nodes;

#pragma warning disable CS8600
public class SpellTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float ManaCost { get; set; }
    public float CastTime { get; set; }
    public SpellNode RootNode { get; set; }
    public bool UseNodeCosts { get; set; } = true;
    public bool CanBeInterrupted { get; set; } = true;

    public SpellTemplate(string name, string description, float manaCost, float castTime, SpellNode rootNode, bool useNodeCosts = true, bool canBeInterrupted = true)
    {
        Name = name;
        Description = description;
        ManaCost = manaCost;
        CastTime = castTime;
        RootNode = rootNode;
        UseNodeCosts = useNodeCosts;
        CanBeInterrupted = canBeInterrupted;
    }

    public float CalculateTotalManaCost()
    {
        return UseNodeCosts ? RootNode.CalculateTotalManaCost() : ManaCost;
    }
}