## Key Methods

- **SetDeclarations**: Replaces existing properties based on importance
  ```csharp
  // Use for explicitly inherited properties
  cssResult.SetDeclarations(inheritPropertiesFromParent);
  ```

- **UpdateDeclarations**: Updates only compatible properties
  ```csharp
  // Use for natural inheritance
  cssResult.UpdateDeclarations(inheritableProperties);
  ```

## Capabilities to Leverage

1. **Shorthand/Longhand Handling**
    - Automatically expands shorthands (`margin`) to longhands (`margin-top`, etc.)
    - Can reconstruct shorthands from longhands
    - Implementation: `SetShorthand()`, `TryCreateShorthand()`

2. **Importance Preservation**
    - Preserves `!important` flags during operations
    - Higher specificity rules in `ChangeDeclarations()`
    - Logic: `(o, n) => !o.IsImportant || n.IsImportant`

3. **Validation & Normalization**
    - Validates values before setting
    - Normalizes values (e.g., `red` → `rgba(255, 0, 0, 1)`)
    - Verification with `if (property.RawValue is not null)`

4. **Property Dependencies**
    - Understands relationships between properties
    - Manages complex properties like `font`
    - Uses `GetMappings()` to find related properties

## Implementation Strategy

- Use `SetDeclarations` for explicit `inherit` keyword properties
- Use `UpdateDeclarations` for natural inheritance
- Delegate complex CSS behaviors to AngleSharp
- Focus implementation on inheritance algorithm
- Avoid reimplementing CSS specification details