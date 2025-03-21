namespace LayoutEngine.Platform.Tests.Unit.Helpers;

using LayoutEngine.Contracts.Platform.Dom;
using LayoutEngine.Contracts.Platform.Updates;

public class TestVisualUpdate : IVisualUpdate
{
    public TestVisualUpdate(Guid id, UpdateType type, IElement element, IReadOnlyList<string> changedProperties)
    {
        Id = id;
        Type = type;
        Element = element;
        ChangedProperties = changedProperties;
        Timestamp = DateTime.UtcNow;
    }

    public Guid Id { get; }
    public UpdateType Type { get; }
    public IElement Element { get; }
    public IReadOnlyList<string> ChangedProperties { get; }
    public DateTime Timestamp { get; }
}