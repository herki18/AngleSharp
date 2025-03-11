```mermaid
graph TB
    A[DOM Element] --> B[ComputedStyleBuilder]
    B --> C[Build & Optimize<br/>PropertyTreeNode]
    C --> D[ComputedStyleFactory]
    D --> E[ComputedStyle<br/>(References PropertyTree Node)]
    E --> F[Layout System]

```