
namespace LadyBird.Libraries.WebView;

using System.Text.Json.Nodes;

public struct DOMNodeProperties
{
    public DOMNodeProperties()
    {
    }

    // C++: enum class Type
    public enum Type
    {
        ComputedStyle,
        Layout,
        UsedFonts,
    }

    // C++: Type type { Type::ComputedStyle };
    public Type NodeType { get; set; } = Type.ComputedStyle;

    // C++: JsonValue properties;
    public JsonNode Properties { get; set; } = default!;
}

