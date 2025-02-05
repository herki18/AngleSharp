/*
Box Model Diagram:

         Margin
       ┌─────────────┐
       │  Border     │
       │ ┌─────────┐ │  <-- Padding
       │ │ Content │ │
       │ └─────────┘ │
       └─────────────┘

    In "content-box":
        Specified width = Content Width.
        Outer width = Content Width + Padding (left + right) + Border (left + right)

    In "border-box":
        Specified width = Outer width.
        Content width = Specified width - Padding - Border.
*/
