# Layout Engine Rules

## Code Structure
- Place all code in the `AngleSharp.LayoutEngine` namespace
- Organize code into these folders:
  - `Box/` - For box model implementations
  - `Core/` - Core layout engine components
  - `DOM/` - DOM interfaces and adapters
  - `FormattingContexts/` - Layout algorithms
  - `Managers/` - Specialty subsystems (margin, float, etc.)
  - `Style/` - Style computation and representation
  - `Util/` - Utilities and helpers
  - `API/` - Public API surface
- Follow Blink architecture patterns with highest priority

## Framework Integration
- Always use AngleSharp.Core and AngleSharp.CSS frameworks where possible
- Never reimplement functionality that exists in AngleSharp.Core or AngleSharp.CSS
- If AngleSharp limitations are found, explicitly note them
- Ensure seamless integration with AngleSharp's DOM and CSS systems

## I/O Requirements
- Input: `IDocument` from AngleSharp
- Output: Layout information that can be consumed by external rendering systems
- All public APIs should accept AngleSharp types as input
- Layout output should include global position, relative position, margin, padding, and border information

## Implementation Guidelines
- Emulate browser implementations as closely as possible
- Implement browser-standard layout algorithms (block, inline, flex)
- Support incremental layout for efficiency
- Implement margin collapsing per CSS specification
- Support all CSS positioning schemes
- Properly handle the box model (content, padding, border, margin)
- Follow CSS specifications precisely
- Prioritize correctness over performance initially
- Rendering/Paint will be implemented outside of the current code base
- Centralize common logic to avoid duplication

Remember to maintain clean interfaces between components and ensure clear separation of concerns.