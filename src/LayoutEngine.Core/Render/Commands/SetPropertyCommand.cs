namespace LayoutEngine.Core.Render.Commands;

using System;
using Layout.Public;
using LayoutEngine.Core.Layout;

/// <summary>
/// Command for setting properties on a visual element
/// </summary>
public class SetPropertyCommand : IRenderCommand
{
    public ILayoutFragment Fragment { get; }
    public RenderCommandType CommandType => RenderCommandType.SetProperty;
    public string PropertyName { get; }
    public object PropertyValue { get; }

    public SetPropertyCommand(ILayoutFragment fragment, string propertyName, object propertyValue)
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        PropertyValue = propertyValue;
    }
}