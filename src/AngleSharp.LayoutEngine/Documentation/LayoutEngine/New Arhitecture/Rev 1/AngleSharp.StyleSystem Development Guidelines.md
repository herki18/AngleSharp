# AngleSharp.StyleSystem Development Guidelines

## Nullable Reference Types

- This project fully embraces C# nullable reference types
- Do not add runtime null checks for non-nullable parameters
- Use the `?` modifier only for truly nullable references
- Follow the compiler warnings regarding potential null references

## Coding Conventions

- Use expression-bodied members for simple methods and properties
- Prefer pattern matching over type checking + casting
- Use `var` only when the type is obvious from the right side
- Always initialize collections in constructors

## Performance Considerations

- Cache expensive calculations when appropriate
- Use StringBuilder for complex string operations
- Consider memory usage in property trees and style resolution

## Documentation Standards

- Place XML documentation comments on interfaces rather than implementations
- All public interface members must be thoroughly documented with XML comments
- Implementation classes should not duplicate interface documentation
- Document complex algorithms with inline comments
- Include references to CSS specifications where relevant

## Testing Requirements

- Every component must have corresponding unit tests
- Test edge cases including circular dependencies and inheritance chains
- Include performance tests for critical paths