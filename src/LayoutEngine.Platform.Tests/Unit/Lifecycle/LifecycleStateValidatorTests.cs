namespace LayoutEngine.Platform.Tests.Unit.Lifecycle;

using LayoutEngine.Contracts.Platform.Lifecycle;
using LayoutEngine.Platform.Lifecycle;
using Xunit;

public class LifecycleStateValidatorTests
{
    private readonly LifecycleStateValidator _validator;

    public LifecycleStateValidatorTests()
    {
        _validator = new LifecycleStateValidator();
    }

    [Theory]
    [InlineData(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.StyleClean, true)]
    [InlineData(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.Disposed, true)]
    [InlineData(DocumentLifecyclePhase.Inactive, DocumentLifecyclePhase.LayoutClean, false)]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.InStyleRecalc, true)]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.LayoutClean, true)]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentLifecyclePhase.RenderReady, false)]
    [InlineData(DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleDirty, true)]
    [InlineData(DocumentLifecyclePhase.InStyleRecalc, DocumentLifecyclePhase.StyleClean, true)]
    [InlineData(DocumentLifecyclePhase.StyleDirty, DocumentLifecyclePhase.StyleClean, true)]
    [InlineData(DocumentLifecyclePhase.StyleDirty, DocumentLifecyclePhase.LayoutClean, false)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.InLayout, true)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.RenderReady, true)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentLifecyclePhase.Disposed, true)]
    [InlineData(DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutDirty, true)]
    [InlineData(DocumentLifecyclePhase.InLayout, DocumentLifecyclePhase.LayoutClean, true)]
    [InlineData(DocumentLifecyclePhase.LayoutDirty, DocumentLifecyclePhase.LayoutClean, true)]
    [InlineData(DocumentLifecyclePhase.LayoutDirty, DocumentLifecyclePhase.RenderReady, false)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.InRender, true)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.StyleClean, true)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentLifecyclePhase.Disposed, true)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderDirty, true)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentLifecyclePhase.RenderReady, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentLifecyclePhase.RenderReady, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentLifecyclePhase.StyleClean, false)]
    [InlineData(DocumentLifecyclePhase.Disposed, DocumentLifecyclePhase.Inactive, false)]
    public void IsValidTransition_ShouldReturnExpectedValue(
        DocumentLifecyclePhase fromPhase,
        DocumentLifecyclePhase toPhase,
        bool expected)
    {
        // Act
        bool result = _validator.IsValidTransition(fromPhase, toPhase);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentOperation.StyleModification, true)]
    [InlineData(DocumentLifecyclePhase.StyleClean, DocumentOperation.LayoutReading, false)]
    [InlineData(DocumentLifecyclePhase.InStyleRecalc, DocumentOperation.StyleReading, false)]
    [InlineData(DocumentLifecyclePhase.InStyleRecalc, DocumentOperation.DomReading, true)]
    [InlineData(DocumentLifecyclePhase.StyleDirty, DocumentOperation.StyleModification, true)]
    [InlineData(DocumentLifecyclePhase.StyleDirty, DocumentOperation.LayoutReading, false)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentOperation.LayoutReading, true)]
    [InlineData(DocumentLifecyclePhase.LayoutClean, DocumentOperation.Rendering, false)]
    [InlineData(DocumentLifecyclePhase.InLayout, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.InLayout, DocumentOperation.LayoutCalculation, true)]
    [InlineData(DocumentLifecyclePhase.InLayout, DocumentOperation.Rendering, false)]
    [InlineData(DocumentLifecyclePhase.LayoutDirty, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.LayoutDirty, DocumentOperation.LayoutCalculation, true)]
    [InlineData(DocumentLifecyclePhase.LayoutDirty, DocumentOperation.Rendering, false)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentOperation.LayoutReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentOperation.RenderReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderReady, DocumentOperation.Rendering, false)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentOperation.LayoutReading, true)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentOperation.RenderReading, true)]
    [InlineData(DocumentLifecyclePhase.InRender, DocumentOperation.Rendering, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentOperation.StyleReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentOperation.LayoutReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentOperation.RenderReading, true)]
    [InlineData(DocumentLifecyclePhase.RenderDirty, DocumentOperation.Rendering, true)]
    [InlineData(DocumentLifecyclePhase.Disposed, DocumentOperation.StyleReading, false)]
    [InlineData(DocumentLifecyclePhase.Disposed, DocumentOperation.DomReading, false)]
    public void IsOperationAllowed_ShouldReturnExpectedValue(
        DocumentLifecyclePhase phase,
        DocumentOperation operation,
        bool expected)
    {
        // Act
        bool result = _validator.IsOperationAllowed(phase, operation);

        // Assert
        Assert.Equal(expected, result);
    }
}