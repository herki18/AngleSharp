There is issuw where we are using CSSSTringValue font will be in "" quatos 

What is proper solution and how sould it behave? 

Below is discussing 

Additional notes should we handle it when we are setting property? Or should we handle it when we are getting it. 


You're asking an important question about the correct state of the font-family value for computation. Let me clarify:

For the font-family value:

1. **In the property tree and CSS value objects**: It should be stored as a `CssStringValue`, which means its `CssText` representation will have quotes (e.g., `"Arial, sans-serif"`).
2. **For computation and when retrieving via the Text.FontFamily property**: It should be the raw string without quotes (e.g., `Arial, sans-serif`).

The issue is in the handling of this string during inheritance. When we get `_parentStyle.Text.FontFamily`, this should already be the raw string without quotes, but if we're seeing `"Arial, sans-serif"` (with quotes), it means we have an issue in how the `TextProperties.FontFamily` getter is implemented.

Let's look at what should be happening in each component:

1. `TextProperties.SetFontFamily(string value)`: Should store the raw string as is
2. `TextProperties.FontFamily` getter: Should return the raw string without any transformation
3. `PropertyTreeNode.SetProperty(name, value)`: For font-family, should use `CssStringValue`
4. When inherited, we need to ensure the correct form is used in each place

If `_parentStyle.Text.FontFamily` is already returning a quoted string, the solution is to strip the quotes when using it:

```csharp
if (!_propertyTree.HasProperty("font-family"))
{
    var parentFontFamily = _parentStyle.Text.FontFamily;
    
    // Strip quotes if present to ensure we have the raw value
    var cleanFontFamily = parentFontFamily.Trim('"', '\'');
    
    // Set the clean value in text properties
    _textProperties.SetFontFamily(cleanFontFamily);
    
    // Use CssStringValue for property tree
    _propertyTree.SetProperty("font-family", new CssStringValue(cleanFontFamily));
}
```

And we should also ensure the `TextProperties.FontFamily` getter isn't adding quotes:

```csharp
// In TextProperties
public string FontFamily => _fontFamily; // Should return the raw value without added quotes
```

The confusion often arises from the different representations needed in different contexts:

- CSS representation (with quotes) for serialization
- Raw string values (without quotes) for programmatic access