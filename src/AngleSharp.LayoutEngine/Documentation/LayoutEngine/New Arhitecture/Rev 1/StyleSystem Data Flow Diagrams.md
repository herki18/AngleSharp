```mermaid
%%{init: {'theme': 'neutral', 'themeVariables': { 'primaryColor': '#f5f5f5', 'primaryTextColor': '#000', 'primaryBorderColor': '#999', 'lineColor': '#666', 'secondaryColor': '#eee', 'tertiaryColor': '#fff'}}}%%

flowchart TD
    %% Initial Style Computation Flow
    subgraph InitialStyleFlow[Initial Style Computation Flow]
        A1[DOM Element] --> A2[StyleTreeResolver]
        A2 --> A3[RuleCollector]
        A3 --> A4[MatchedRules]
        A4 --> A5[CascadeResolver]
        A5 --> A6[CascadedStyle]
        A6 --> A7[InheritanceProcessor]
        A7 --> A8[InheritedStyle]
        A8 --> A9[ComputedStyleBuilder]
        A9 --> A10[VariableResolver & ValueCalculator]
        A10 --> A11[PropertyTreeManager]
        A11 --> A12[StylePropertyMapper]
        A12 --> A13[ComputedStyle]
        A13 --> A14[Layout System]
    end

    %% Style Recalculation Flow
    subgraph RecalcFlow[Style Recalculation Flow]
        B1[DOM/Style Change] --> B2[StyleInvalidationTracker]
        B2 --> B3[Affected Elements]
        B3 --> B4[StyleTreeResolver]
        B4 --> B5{Check Cache}
        B5 -->|Cache Miss| B6[Compute New Style]
        B5 -->|Cache Hit| B7[Reuse Cached Style]
        B6 --> B8[Update ComputedStyle]
        B7 --> B8
        B8 --> B9[Notify Layout System]
    end

    %% Multi-Threaded Style Flow
    subgraph ThreadedFlow[Multi-Threaded Style Flow]
        C1[DOM Change] --> C2[DocumentLifecycleCoordinator]
        C2 --> C3[StyleRecalcScheduler]
        C3 --> C4{Analyze Element Priority}
        C4 -->|Critical Path| C5[MainThreadStyleWork]
        C4 -->|Non-Critical Path| C6[WorkerThreadStylePool]
        C5 --> C7[Style Computation]
        C6 --> C8[Parallel Style Computation]
        C7 --> C9[Synchronize Results]
        C8 --> C9
        C9 --> C10[Update ComputedStyle]
        C10 --> C11[Notify Layout]
    end

    %% Animation-Aware Style Processing
    subgraph AnimationFlow[Animation-Aware Style Processing]
        D1[Animation Update] --> D2[AnimationStyleEngine]
        D2 --> D3{Is Compositor-Only?}
        D3 -->|Yes| D4[Update Compositor Values]
        D3 -->|No| D5[Identify Animating Properties]
        D4 --> D6[Skip Main Thread]
        D5 --> D7[StyleEngine]
        D7 --> D8[Perform Minimal Style Update]
        D8 --> D9[Update ComputedStyle]
        D6 --> D10[Render Frame]
        D9 --> D10
    end

    %% Display-Locked Element Flow
    subgraph DisplayLockFlow[Display-Locked Element Flow]
        E1[DOM Element] --> E2[DisplayLockManager]
        E2 --> E3{Is Visible?}
        E3 -->|Yes| E4[Normal Style Computation]
        E3 -->|No| E5[Defer Computation]
        E5 --> E6[Record for Later]
        E6 --> E7{Scrolled Into View?}
        E7 -->|Yes| E8[Prioritize Computation]
        E4 --> E9[Update ComputedStyle]
        E8 --> E9
    end

    %% Variable Resolution Flow
    subgraph VariableFlow[Variable Resolution Flow]
        F1[CSS Variable Reference] --> F2[VariableResolver]
        F2 --> F3{Check Variable Cache}
        F3 -->|Cache Miss| F4[Resolve Dependencies]
        F3 -->|Cache Hit| F5[Return Cached Value]
        F4 --> F6{Check for Circular References}
        F6 -->|No Circularity| F7[Resolve Nested Variables]
        F6 -->|Circular Reference| F8[Use Fallback or Return NULL]
        F7 --> F9[Compute Final Value]
        F9 --> F10[Cache Result]
        F10 --> F11[Return Resolved Value]
        F5 --> F11
        F8 --> F11
    end

    %% Logical Properties Flow
    subgraph LogicalPropsFlow[Logical Properties Flow]
        G1[Logical Property] --> G2[StylePropertyMapper]
        G2 --> G3[Check Writing Mode]
        G3 --> G4[Determine Physical Mapping]
        G4 --> G5[Transform to Physical Properties]
        G5 --> G6[Store in ComputedStyle]
        G6 --> G7[Layout System Access]
    end
```