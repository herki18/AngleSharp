# CascadeResolver Test Plan

## Core Functionality Tests

1. **ResolveCascade_WithEmptyRules_ReturnsEmptyDeclaration**
   - **Purpose**: Verify that when no CSS rules match an element, the resolver returns an empty but valid style declaration.
   - **Why it matters**: Edge case handling is important; elements without styles should get a valid empty declaration rather than null.

2. **ResolveCascade_WithSingleRule_ReturnsRuleProperties**
   - **Purpose**: Verify that a single matching rule's properties are correctly applied to the result.
   - **Why it matters**: Basic functionality test - confirms the resolver can transfer properties from a rule to the result.

## CSS Cascade Order Tests

1. **ResolveCascade_WithMultipleRules_AppliesInCorrectOrder**
   - **Purpose**: Verify that rules from different origins (user agent, user, author) are applied in the correct cascade order.
   - **Why it matters**: The cascade order is fundamental to CSS - author styles should override user styles, which override user agent styles.

2. **ResolveCascade_WithDifferentSpecificity_PrioritizesHigherSpecificity**
   - **Purpose**: Verify that within the same origin, more specific selectors override less specific ones.
   - **Why it matters**: Specificity is a core concept in CSS that determines which styles take precedence.

3. **ResolveCascade_WithSameSpecificityDifferentOrder_PrioritizesLaterRule**
   - **Purpose**: Verify that when rules have the same origin and specificity, the later rule in source order wins.
   - **Why it matters**: Source order is the final tie-breaker in CSS cascade algorithm.

## Special CSS Features Tests

1. **ResolveCascade_WithInlineStyles_AppliesInlineStyles**
   - **Purpose**: Verify that inline styles (from the style attribute) override normal stylesheet rules.
   - **Why it matters**: Inline styles have higher precedence than normal stylesheet rules in the cascade.

2. **ResolveCascade_ImportantAuthorOverridesInlineStyle**
   - **Purpose**: Verify that !important rules from author stylesheets override inline styles.
   - **Why it matters**: The !important flag changes the normal cascade order, putting these rules above inline styles.

3. **ResolveCascade_UserAgentImportantTrumpsEverythingButUserImportant**
   - **Purpose**: Verify that !important rules are applied in the correct order across origins.
   - **Why it matters**: The cascade order for !important rules is the reverse of normal rules (user agent !important > author !important > user !important).

4. **ResolveCascade_PreservesImportantFlag**
   - **Purpose**: Verify that when a property with !important is resolved through the cascade, it keeps its important flag.
   - **Why it matters**: The !important flag must be preserved to ensure the property has the correct precedence in future operations.
   - **Note**: This test is currently failing and will be revisited. It may require creating a real parsed stylesheet rather than mocks.

## Error Handling Tests

1. **ResolveCascade_HandlesNullRuleStyle**
    - **Purpose**: Verify that the resolver can handle rules with null style property without throwing exceptions.
    - **Why it matters**: Robust error handling prevents crashes when dealing with malformed or incomplete rules.

## Additional Tests to Consider

1. **ResolveCascade_WithShorthandAndLonghandProperties**
    - **Purpose**: Verify shorthand properties (like 'margin') and their longhand counterparts ('margin-top', etc.) are handled correctly.
    - **Why it matters**: CSS has many shorthand properties that resolve to multiple longhand properties.

2. **ResolveCascade_WithInheritKeyword**
    - **Purpose**: Verify the 'inherit' keyword properly causes properties to inherit from the parent element.
    - **Why it matters**: The 'inherit' keyword changes the normal inheritance behavior of properties.

3. **ResolveCascade_WithInitialKeyword**
    - **Purpose**: Verify the 'initial' keyword correctly resets properties to their initial values.
    - **Why it matters**: The 'initial' keyword is important for resetting properties to specification defaults.

4. **ResolveCascade_WithUnsetKeyword**
    - **Purpose**: Verify the 'unset' keyword behaves like 'inherit' for inherited properties and 'initial' for non-inherited properties.
    - **Why it matters**: The 'unset' keyword provides a universal way to reset properties based on their inherent nature.