# Development Guidelines

## Nullable Reference Types

- Fully embrace C# nullable reference types
- Do not add runtime null checks for non-nullable parameters
- Use the `?` modifier only for truly nullable references
- Follow the compiler warnings regarding potential null references

## File Organization

- Use file-scoped namespaces (introduced in C# 10)
- Keep one primary class per file
- Group related small classes and interfaces in the same file when appropriate
- Follow the project's folder structure for organization

## Documentation Standards

- Place XML documentation comments on interfaces rather than implementations
- All public interface members must be thoroughly documented with XML comments
- Implementation classes should not duplicate interface documentation
- Document complex algorithms with inline comments

## Coding Conventions

- Use expression-bodied members for simple methods and properties
- Prefer pattern matching over type checking + casting
- Use `var` only when the type is obvious from the right side
- Always initialize collections in constructors
- Follow C# naming conventions (Pascal case for public members, camel case for parameters)
- Use `readonly` for fields that don't change after initialization

## Performance Considerations

- Cache expensive calculations when appropriate
- Use StringBuilder for complex string operations
- Be mindful of memory usage in data structures
- Prefer Dictionary lookup over linear searches
- Consider allocation patterns and garbage collection in performance-critical paths

## Testing Requirements

- Every component must have corresponding unit tests
- Test edge cases thoroughly
- Include performance tests for critical paths
- Mock dependencies for isolated testing
- Use test-driven development for complex algorithms