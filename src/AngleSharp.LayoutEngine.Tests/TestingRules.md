# Layout Engine Testing Rules

## Testing Organization
- Organize tests in `AngleSharp.LayoutEngine.Tests/` with matching structure to the main codebase
- Create separate test classes for each component being tested
- Group related tests within the same file using nested classes when appropriate

## Testing Framework and Style
- Use NUnit for all tests
- Follow Microsoft test naming convention: `[UnitOfWork]_[StateUnderTest]_[ExpectedBehavior]`
  - Example: `BoxModel_WithNestedElements_CalculatesCorrectDimensions`
  - Example: `MarginCollapse_WithEmptyBlocks_CollapsesCorrectly`
- Use `[TestFixture]` attribute for test classes
- Use `[Test]` attribute for test methods
- Use `[TestCase]` attribute for parameterized tests
- Use `[SetUp]` and `[TearDown]` for test initialization and cleanup

## Test Types
- Unit Tests: Test individual components in isolation
- Integration Tests: Test interactions between components
- Behavioral Tests: Test end-to-end behavior from HTML to layout
- Visual Tests: Compare layout results against expected values
- Performance Tests: Ensure layout operations meet performance requirements

## Testing Strategies
- Create test helpers to simplify test setup
- Test against reference browser implementations when possible
- Test complex edge cases (margin collapsing, float interactions, etc.)
- Test with real-world HTML/CSS examples
- Test with invalid or unexpected inputs
- Test with extreme values (very large/small elements, deeply nested structures)

## Visual Regression Testing
- Create baseline layout results for comparison
- Compare layout coordinates, dimensions, and box properties
- Use tolerance values for floating-point comparisons
- Include comprehensive test coverage for all layout models

## Documentation
- Document test assumptions and edge cases
- Explain complex test setups
- Reference CSS specifications in tests when appropriate
- Include comments for complex assertions

Always ensure tests are deterministic, repeatable, and independent of each other.