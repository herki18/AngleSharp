# AngleSharp.StyleSystem Development Guide

## Component Architecture

The StyleSystem follows a modular architecture with these key components:

1. **StyleEngine**: Main orchestrator for style computation
2. **StyleTreeResolver**: Handles element tree traversal and style computation scheduling
3. **RuleCollector**: For matching elements to CSS rules
4. **CascadeResolver**: For resolving property precedence and conflicts
5. **InheritanceProcessor**: For handling property inheritance
6. **ComputedStyleBuilder**: For building the final computed style
7. **VariableResolver**: For CSS custom property resolution
8. **ValueCalculator**: For value computation and unit conversion
9. **PropertyTreeManager**: For efficient style storage and sharing
10. **StylePropertyMapper**: For logical-to-physical property mapping

## Component Boundaries

- Each component should have a single responsibility
- Components should communicate through well-defined interfaces
- Avoid circular dependencies between components
- Use the provided architecture diagram for guidance

## AngleSharp Integration

- Use existing AngleSharp types and functionality rather than reimplementing them
- Follow AngleSharp's patterns for CSS property handling
- Properly utilize AngleSharp's declarations handling methods:
    - 'SetDeclarations' for explicit inheritance
    - 'UpdateDeclarations' for natural inheritance
- Maintain compatibility with AngleSharp's DOM and CSS models

## Implementation Patterns

- **Adapter Pattern**: Create adapters to convert between AngleSharp's value system and layout-optimized representations
- **Extension Methods**: Extend AngleSharp's interfaces rather than recreating them
- **Composition Over Inheritance**: Use AngleSharp's objects as internal components

## Avoid Duplication

Avoid duplicating functionality already present in AngleSharp, including:

- CSS value parsing
- Selector matching (use AngleSharp's selector engine)
- CSS property definitions
- Media query evaluation

## CSS Specification References

- Include references to relevant CSS specifications in documentation
- Follow CSS specifications for implementation behavior
- Document deviations from specifications when necessary