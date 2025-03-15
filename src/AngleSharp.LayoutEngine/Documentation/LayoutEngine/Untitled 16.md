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

    %% Dependency Injection Flow
    subgraph DIFlow[Dependency Injection Flow]
        D1[Application] --> D2[ServiceCollection]
        D2 --> D3[Register StyleSystem Components]
        D3 --> D4[Build ServiceProvider]
        D4 --> D5[Create StyleSystemService]
        D5 --> D6[Initialize StyleEngine]
        D6 --> D7{Ready To Process?}
        D7 -->|Yes| D8[Process Style Requests]
        D7 -->|No| D9[Pending Initialization]
        D9 --> D6
    end

    %% Service Resolution Flow
    subgraph ResolutionFlow[Service Resolution Flow]
        E1[BrowsingContext] --> E2[GetStyleSystemService]
        E2 --> E3[StyleSystemService]
        E3 --> E4[IServiceProvider]
        E4 --> E5[Resolve Request]
        E5 --> E6{Service Available?}
        E6 -->|Yes| E7[Return Service]
        E6 -->|No| E8[Create if Needed]
        E8 --> E9{Can Create?}
        E9 -->|Yes| E10[Initialize Service]
        E9 -->|No| E11[Return Null]
        E10 --> E7
    end

    %% Animation-Aware Style Processing
    subgraph AnimationFlow[Animation-Aware Style Processing]
        F1[Animation Update] --> F2[AnimationStyleEngine]
        F2 --> F3{Is Compositor-Only?}
        F3 -->|Yes| F4[Update Compositor Values]
        F3 -->|No| F5[Identify Animating Properties]
        F4 --> F6[Skip Main Thread]
        F5 --> F7[StyleEngine]
        F7 --> F8[Perform Minimal Style Update]
        F8 --> F9[Update ComputedStyle]
        F6 --> F10[Render Frame]
        F9 --> F10
    end

    %% Display-Locked Element Flow
    subgraph DisplayLockFlow[Display-Locked Element Flow]
        G1[DOM Element] --> G2[DisplayLockManager]
        G2 --> G3{Is Visible?}
        G3 -->|Yes| G4[Normal Style Computation]
        G3 -->|No| G5[Defer Computation]
        G5 --> G6[Record for Later]
        G6 --> G7{Scrolled Into View?}
        G7 -->|Yes| G8[Prioritize Computation]
        G4 --> G9[Update ComputedStyle]
        G8 --> G9
    end

    %% Variable Resolution Flow
    subgraph VariableFlow[Variable Resolution Flow]
        H1[CSS Variable Reference] --> H2[VariableResolver]
        H2 --> H3{Check Variable Cache}
        H3 -->|Cache Miss| H4[Resolve Dependencies]
        H3 -->|Cache Hit| H5[Return Cached Value]
        H4 --> H6{Check for Circular References}
        H6 -->|No Circularity| H7[Resolve Nested Variables]
        H6 -->|Circular Reference| H8[Use Fallback or Return NULL]
        H7 --> H9[Compute Final Value]
        H9 --> H10[Cache Result]
        H10 --> H11[Return Resolved Value]
        H5 --> H11
        H8 --> H11
    end

    %% Logical Properties Flow
    subgraph LogicalPropsFlow[Logical Properties Flow]
        I1[Logical Property] --> I2[StylePropertyMapper]
        I2 --> I3[Check Writing Mode]
        I3 --> I4[Determine Physical Mapping]
        I4 --> I5[Transform to Physical Properties]
        I5 --> I6[Store in ComputedStyle]
        I6 --> I7[Layout System Access]
    end

    %% Document Lifecycle Flow
    subgraph LifecycleFlow[Document Lifecycle Flow]
        J1[Document Created/Changed] --> J2[DocumentLifecycleCoordinator]
        J2 --> J3[Notify StyleSystemService]
        J3 --> J4[Initialize/Update Style Engine]
        J4 --> J5[Register DOM Observers]
        J5 --> J6[Initialize StyleSheetManager]
        J6 --> J7[Wait for ReadyState Changes]
        J7 --> J8{Ready State}
        J8 -->|interactive| J9[Initial Style Computation]
        J8 -->|complete| J10[Full Style Computation]
        J9 --> J11[Optimize StyleTree]
        J10 --> J11
    end
```