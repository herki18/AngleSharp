# AngleSharp.StyleSystem Testing Reference

## Shared Testing Infrastructure

### Base Class Usage

```csharp
[TestFixture]
public class YourTestClass : StyleSystemTestBase
{
    [SetUp]
    public void Setup()
    {
        // Initialize the base fixture
        SetupFixture();
        
        // Setup additional test objects
        // ...
    }
    
    [Test]
    public void YourTest()
    {
        // Use the shared infrastructure
        // ...
    }
}
```

### Key Helper Methods

| Method | Purpose | Example |
|--------|---------|---------|
| `CreateMatchedRule()` | Creates a MatchedRule from CSS text | `CreateMatchedRule("color: red", StylesheetOrigin.Author, Priority.OneTag)` |
| `ParseCssTextToProperties()` | Parses CSS text to a list of properties | `ParseCssTextToProperties("color: red; font-size: 16px")` |
| `CreateTestElement()` | Creates an element with customizable properties | `CreateTestElement("div", "myId", "my-class", parentElement)` |

## AutoFixture Usage

### Accessing the Fixture

```csharp
// The Fixture is available as a protected property
var element = Fixture.Create<IElement>();
var context = Fixture.Create<IBrowsingContext>();
```

### Customizing Created Objects

```csharp
// Register a specific instance
Fixture.Inject(_renderDevice);

// Register a factory function
Fixture.Register<ICssStyleDeclaration>(() => new TestCssStyleDeclaration());
```

### Creating Objects with Specific Properties

```csharp
// Create an element with customization
var element = Fixture.Build<IElement>()
    .With(e => e.TagName, "div")
    .With(e => e.Id, "custom-id")
    .Create();
```

## NSubstitute Usage

### Creating Substitutes Directly

```csharp
var renderDevice = Substitute.For<IRenderDevice>();
renderDevice.ViewPortWidth.Returns(1024);
renderDevice.ViewPortHeight.Returns(768);
```

### Using Substitutes from AutoFixture

```csharp
// Get a substitute from AutoFixture
var element = Fixture.Create<IElement>();

// Configure it
element.GetAttribute("style").Returns("color: red");
```

### Verifying Calls

```csharp
// Verify a method was called
invalidationTracker.Received().MarkAsDeviceDependent(element);

// Verify a method was called with specific arguments
resolver.Received().ResolveCascade(Arg.Is<IEnumerable<MatchedRule>>(r => r.Count() == 3), element);
```

## CSS Test Implementations

### Available Test Implementations

| Class | Purpose |
|-------|---------|
| `TestCssStyleDeclaration` | Implements ICssStyleDeclaration for tests |
| `TestCssProperty` | Implements ICssProperty for tests |
| `TestCssValue` | Implements ICssValue for tests |
| `TestCssStyleRule` | Implements ICssStyleRule for tests |

### TestCssStyleDeclaration Usage

```csharp
// Create an empty declaration
var declaration = new TestCssStyleDeclaration();

// Create with CSS text
declaration.ParseCssText("color: red; font-size: 16px");

// Create with properties
var properties = new List<ICssProperty> { /* properties */ };
var declaration = new TestCssStyleDeclaration(properties);
```

### TestCssProperty Usage

```csharp
// Create a property
var property = new TestCssProperty("color", "red", isImportant: false);

// Access property members
string name = property.Name;
string value = property.Value;
bool isImportant = property.IsImportant;
ICssValue rawValue = property.RawValue;
```

## Common Test Patterns

### Testing Priority Rules

```csharp
// Create rules with different priorities
var lowRule = CreateMatchedRule("color: red", StylesheetOrigin.Author, Priority.OneTag);
var highRule = CreateMatchedRule("color: blue", StylesheetOrigin.Author, Priority.OneId);

// Create a list of rules
var rules = new List<MatchedRule> { lowRule, highRule };

// Resolve cascade
var result = resolver.ResolveCascade(rules, element);

// Assert expected outcome
Assert.That(result.GetPropertyValue("color"), Is.EqualTo("blue"));
```

### Testing Property Inheritance

```csharp
// Create parent style
var parentDeclaration = new TestCssStyleDeclaration();
parentDeclaration.ParseCssText("color: red");
var parentStyle = CreateComputedStyle(parentElement, parentDeclaration);

// Create child style with parent reference
var childDeclaration = new TestCssStyleDeclaration();
var childStyle = CreateComputedStyle(childElement, childDeclaration, parentStyle);

// Assert inheritance
Assert.That(childStyle.GetPropertyValue("color"), Does.Contain("red"));
```

### Testing ComputedStyle Properties

```csharp
// Create style with properties
var declaration = new TestCssStyleDeclaration();
declaration.ParseCssText("display: flex; position: absolute; color: blue");
var style = CreateComputedStyle(element, declaration);

// Assert computed values
Assert.That(style.Display, Is.EqualTo(DisplayMode.Flex));
Assert.That(style.Position, Is.EqualTo(PositionMode.Absolute));
Assert.That(style.GetPropertyValue("color"), Does.Contain("blue"));
```

### Testing Device-Dependent Properties

```csharp
// Create style with device-dependent properties
var declaration = new TestCssStyleDeclaration();
declaration.ParseCssText("width: 50vw; height: 30vh");

// Configure render device
renderDevice.ViewPortWidth.Returns(1000);
renderDevice.ViewPortHeight.Returns(800);

// Create computed style
var style = CreateComputedStyle(element, declaration);

// Verify device dependency tracking
invalidationTracker.Received().MarkAsDeviceDependent(element);
```