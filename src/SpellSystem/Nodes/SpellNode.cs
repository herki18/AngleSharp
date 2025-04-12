namespace SpellSystem.Nodes;

#pragma warning disable CS8600
public abstract class SpellNode
{
    public int NodeID { get; set; }
    public NodeType NodeType { get; protected set; }
    public List<SpellNode> ChildNodes { get; } = new List<SpellNode>();
    public float ManaCost { get; set; } = 0f;

    protected Dictionary<string, object> parameters = new Dictionary<string, object>();

    public T? GetParam<T>(string key, T? defaultValue = default)
    {
        if (parameters.TryGetValue(key, out object? value) && value is T tValue)
        {
            return tValue;
        }
        return defaultValue;
    }

    public void SetParam<T>(string key, T value)
    {
        parameters[key] = value!;
    }

    public void AddChild(SpellNode node)
    {
        if (!ChildNodes.Contains(node))
        {
            ChildNodes.Add(node);
        }
    }

    public abstract bool CanExecute(SpellContext context);
    public abstract void Execute(SpellContext context);

    protected void ExecuteChildren(SpellContext context)
    {
        foreach (var child in ChildNodes)
        {
            if (child.CanExecute(context))
            {
                child.Execute(context);
            }
        }
    }

    public float CalculateTotalManaCost()
    {
        float totalCost = ManaCost;
        foreach (var child in ChildNodes)
        {
            totalCost += child.CalculateTotalManaCost();
        }
        return totalCost;
    }
}