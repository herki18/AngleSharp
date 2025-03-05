```meridan
graph TB
    %% Main Modules
    subgraph StyleComputation["Style Computation Module (Implemented)"]
        SCE[StyleComputationEngine]
        SM[StyleSheetManager]
        SELM[SelectorMatcher]
        CR[CascadeResolver]
        IP[InheritanceProcessor]
        VC[ValueComputer]
    end
    
    subgraph DocumentLifecycle["Document Lifecycle Module (Planned)"]
        DLM[DocumentLifecycleManager]
        MOA[MutationObserverAdapter]
        IM[InvalidationManager]
        SIT[StyleInvalidationTracker]
        LIT[LayoutInvalidationTracker]
        SS[SchedulingService]
    end
    
    subgraph CacheModule["Cache Module (Implemented)"]
        LECM[LayoutEngineCacheManager]
        SC[StyleCache]
        FC[FragmentCache]:::new
        ISC[IntrinsicSizeCache]:::new
        CDT[CacheDependencyTracker]
        EDT[EnhancedDependencyTracker]
    end
    
    subgraph LayoutEngine["Layout Engine Module (LayoutNG-Inspired)"]
        LE[LayoutEngine]
        FCTF[FormattingContextFactory]:::new
        LFT[LayoutFragmentTree]:::new
        BGR[BoxGeometryResolver]:::new
        
        subgraph FormattingContexts[Formatting Contexts]
            BFC[BlockFormattingContext]:::new
            IFC[InlineFormattingContext]:::new
            FFC[FlexFormattingContext]:::new
            GFC[GridFormattingContext]:::new
        end
        
        subgraph LayoutComponents[Layout Components]
            ISC2[IntrinsicSizesCalculator]:::new
            MCE[MarginCollapsingEngine]:::new
            LB[LineBreaker]:::new
            BFE[BlockFragmentationEngine]:::new
        end
        
        subgraph DataStructures[Data Structures]
            CS[ConstraintSpace]:::new
            LF[LayoutFragment]:::new
            LR[LayoutResult]:::new
            FB[FragmentBuilder]:::new
        end
    end
    
    %% Main Flow Connections
    SCE --> LE
    DLM --> LE
    LE --> LECM
    
    %% Style to Layout
    SCE -- "Computed Styles" --> LE
    SCE -- "Logical Properties" --> CS
    
    %% Layout Factory
    LE --> FCTF
    FCTF --> FormattingContexts
    FCTF --> CS
    
    %% Layout Components
    BFC --> MCE
    BFC --> BGR
    IFC --> LB
    BFC --> LFT
    IFC --> LFT
    FFC --> LFT
    GFC --> LFT
    
    %% Layout Tree Building
    LFT --> FB
    FB --> LF
    LE --> LR
    LF --> LR
    
    %% Cache Integration
    LECM -- "Cache Fragment" --> FC
    LECM -- "Cache Sizes" --> ISC
    LE -- "Query Cache" --> LECM
    
    %% Invalidation
    LIT -- "Invalidate Layout" --> LECM
    LIT -- "Invalidate Sizes" --> ISC
    MOA -- "Detect Changes" --> IM
    IM --> SIT
    IM --> LIT
    
    %% Legend
    classDef implemented fill:#d4edda,stroke:#155724
    classDef planned fill:#fff3cd,stroke:#856404
    classDef new fill:#cce5ff,stroke:#004085
    
    class SCE,SM,SELM,CR,IP,VC,LECM,SC,CDT implemented
    class DLM,MOA,IM,SIT,LIT,SS planned
    class LE,FCTF,LFT,BGR,BFC,IFC,FFC,GFC,ISC2,MCE,LB,BFE,CS,LF,LR,FB,FC,ISC,EDT new
```