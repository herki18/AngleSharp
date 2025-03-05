<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 650">
  <!-- Background and border -->
  <rect width="900" height="650" fill="#f8f9fa" rx="10" ry="10" stroke="#d0d7de" stroke-width="2"/>
  
  <!-- Title -->
  <text x="450" y="40" font-family="Arial, sans-serif" font-size="24" text-anchor="middle" font-weight="bold" fill="#24292e">Document Lifecycle-Driven Invalidation System</text>
  
  <!-- Document Lifecycle Manager -->
  <rect x="350" y="70" width="200" height="80" rx="5" ry="5" fill="#d1ecf1" stroke="#0c5460" stroke-width="2"/>
  <text x="450" y="100" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#0c5460">DocumentLifecycleManager</text>
  <text x="450" y="125" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#0c5460">Manages document state</text>
  <text x="450" y="140" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#0c5460">and lifecycle transitions</text>
  
  <!-- Mutation Observer Adapter -->
  <rect x="50" y="200" width="180" height="80" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="140" y="230" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">MutationObserverAdapter</text>
  <text x="140" y="255" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">Adapts AngleSharp's</text>
  <text x="140" y="270" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">MutationObserver</text>
  
  <!-- Mutation Batch Processor -->
  <rect x="50" y="320" width="180" height="80" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="140" y="350" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">MutationBatchProcessor</text>
  <text x="140" y="375" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">Batches, optimizes,</text>
  <text x="140" y="390" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">and processes mutations</text>
  
  <!-- Invalidation Manager -->
  <rect x="350" y="200" width="200" height="80" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2"/>
  <text x="450" y="230" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">InvalidationManager</text>
  <text x="450" y="255" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">Determines what needs</text>
  <text x="450" y="270" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">invalidation and recalculation</text>
  
  <!-- Style Invalidation Tracker -->
  <rect x="350" y="320" width="200" height="60" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2"/>
  <text x="450" y="345" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">StyleInvalidationTracker</text>
  <text x="450" y="365" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">Tracks style dirty elements</text>
  
  <!-- Layout Invalidation Tracker -->
  <rect x="350" y="390" width="200" height="60" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2" stroke-dasharray="5,5"/>
  <text x="450" y="415" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">LayoutInvalidationTracker</text>
  <text x="450" y="435" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">Tracks layout dirty elements</text>
  
  <!-- Visual Invalidation Tracker -->
  <rect x="350" y="460" width="200" height="60" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2" stroke-dasharray="5,5"/>
  <text x="450" y="485" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">VisualInvalidationTracker</text>
  <text x="450" y="505" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">Tracks visual dirty areas</text>
  
  <!-- Enhanced Dependency Tracker -->
  <rect x="670" y="200" width="180" height="80" rx="5" ry="5" fill="#f8d7da" stroke="#721c24" stroke-width="2"/>
  <text x="760" y="230" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#721c24">EnhancedDependencyTracker</text>
  <text x="760" y="255" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">Tracks relationships</text>
  <text x="760" y="270" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">between elements</text>
  
  <!-- Update Scheduler -->
  <rect x="350" y="560" width="200" height="60" rx="5" ry="5" fill="#e2e3e5" stroke="#383d41" stroke-width="2"/>
  <text x="450" y="585" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#383d41">UpdateScheduler</text>
  <text x="450" y="605" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#383d41">Controls when updates occur</text>
  
  <!-- Integration Components -->
  <rect x="670" y="320" width="180" height="60" rx="5" ry="5" fill="#f8d7da" stroke="#721c24" stroke-width="2"/>
  <text x="760" y="345" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#721c24">Selector Dependencies</text>
  <text x="760" y="365" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">Tracks selector relations</text>
  
  <rect x="670" y="390" width="180" height="60" rx="5" ry="5" fill="#f8d7da" stroke="#721c24" stroke-width="2"/>
  <text x="760" y="415" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#721c24">Style Dependencies</text>
  <text x="760" y="435" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">Tracks style inheritance</text>
  
  <rect x="670" y="460" width="180" height="60" rx="5" ry="5" fill="#f8d7da" stroke="#721c24" stroke-width="2"/>
  <text x="760" y="485" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#721c24">Variable Dependencies</text>
  <text x="760" y="505" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">Tracks CSS variable usage</text>
  
  <!-- External Integration Boxes -->
  <rect x="50" y="470" width="180" height="60" rx="5" ry="5" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="140" y="495" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#004085">StyleComputationEngine</text>
  <text x="140" y="515" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">(Existing Component)</text>
  
  <rect x="50" y="540" width="180" height="60" rx="5" ry="5" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="140" y="565" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#004085">Cache System</text>
  <text x="140" y="585" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">(Existing Component)</text>
  
  <rect x="670" y="540" width="180" height="60" rx="5" ry="5" fill="#cce5ff" stroke="#004085" stroke-width="2" stroke-dasharray="5,5"/>
  <text x="760" y="565" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#004085">Layout Engine</text>
  <text x="760" y="585" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">(Future Component)</text>
  
  <!-- Connection Lines -->
  <!-- DocumentLifecycleManager connections -->
  <line x1="450" y1="150" x2="450" y2="200" stroke="#0c5460" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- MutationObserverAdapter to MutationBatchProcessor -->
  <line x1="140" y1="280" x2="140" y2="320" stroke="#856404" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- MutationBatchProcessor to InvalidationManager -->
  <line x1="230" y1="360" x2="350" y2="240" stroke="#856404" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- InvalidationManager to Trackers -->
  <line x1="450" y1="280" x2="450" y2="320" stroke="#155724" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="450" y1="380" x2="450" y2="390" stroke="#155724" stroke-width="2" marker-end="url(#arrowhead)" stroke-dasharray="5,5"/>
  <line x1="450" y1="450" x2="450" y2="460" stroke="#155724" stroke-width="2" marker-end="url(#arrowhead)" stroke-dasharray="5,5"/>
  
  <!-- InvalidationManager to DependencyTracker -->
  <line x1="550" y1="240" x2="670" y2="240" stroke="#155724" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- DependencyTracker to Dependencies -->
  <line x1="760" y1="280" x2="760" y2="320" stroke="#721c24" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="760" y1="380" x2="760" y2="390" stroke="#721c24" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="760" y1="450" x2="760" y2="460" stroke="#721c24" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- InvalidationManager to UpdateScheduler -->
  <line x1="450" y1="520" x2="450" y2="560" stroke="#155724" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- UpdateScheduler to StyleComputationEngine -->
  <line x1="350" y1="590" x2="230" y2="530" stroke="#383d41" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- UpdateScheduler to Layout Engine -->
  <line x1="550" y1="590" x2="670" y2="570" stroke="#383d41" stroke-width="2" marker-end="url(#arrowhead)" stroke-dasharray="5,5"/>
  
  <!-- StyleComputationEngine to Cache -->
  <line x1="140" y1="530" x2="140" y2="540" stroke="#004085" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- Arrows and marker definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#000"/>
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="50" y="70" width="15" height="15" fill="#d1ecf1" stroke="#0c5460" stroke-width="1"/>
  <text x="75" y="83" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Core Coordination</text>
  
  <rect x="50" y="95" width="15" height="15" fill="#fff3cd" stroke="#856404" stroke-width="1"/>
  <text x="75" y="108" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Mutation Handling</text>
  
  <rect x="50" y="120" width="15" height="15" fill="#d4edda" stroke="#155724" stroke-width="1"/>
  <text x="75" y="133" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Invalidation System</text>
  
  <rect x="50" y="145" width="15" height="15" fill="#f8d7da" stroke="#721c24" stroke-width="1"/>
  <text x="75" y="158" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Dependency Tracking</text>
  
  <rect x="200" y="70" width="15" height="15" fill="#e2e3e5" stroke="#383d41" stroke-width="1"/>
  <text x="225" y="83" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Scheduling</text>
  
  <rect x="200" y="95" width="15" height="15" fill="#cce5ff" stroke="#004085" stroke-width="1"/>
  <text x="225" y="108" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Existing Components</text>
  
  <rect x="200" y="120" width="15" height="15" fill="#cce5ff" stroke="#004085" stroke-width="1" stroke-dasharray="5,5"/>
  <text x="225" y="133" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Future Components</text>
</svg>