<svg viewBox="0 0 1000 800" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="1000" height="800" fill="#f8f9fa" />
  
  <!-- System Title -->
  <text x="500" y="30" font-family="Arial" font-size="24" font-weight="bold" text-anchor="middle" fill="#333333">AngleSharp Layout Engine - Complete System Architecture</text>
  
  <!-- AngleSharp Core -->
  <rect x="325" y="60" width="350" height="80" rx="10" ry="10" fill="#e6e6ff" stroke="#000033" stroke-width="2" />
  <text x="500" y="105" font-family="Arial" font-size="18" font-weight="bold" text-anchor="middle" fill="#000033">AngleSharp Core (DOM, CSS Parser)</text>
  
  <!-- Module Boundaries -->
  <!-- Style Computation Module -->
  <rect x="50" y="180" width="400" height="280" rx="10" ry="10" fill="#e6f3ff" stroke="#0066cc" stroke-width="2" opacity="0.8" />
  <text x="250" y="205" font-family="Arial" font-size="18" font-weight="bold" text-anchor="middle" fill="#0066cc">Style Computation Module</text>
  
  <!-- Document Lifecycle Module -->
  <rect x="550" y="180" width="400" height="280" rx="10" ry="10" fill="#e6ffe6" stroke="#006600" stroke-width="2" opacity="0.8" />
  <text x="750" y="205" font-family="Arial" font-size="18" font-weight="bold" text-anchor="middle" fill="#006600">Document Lifecycle Module</text>
  
  <!-- Caching Module -->
  <rect x="50" y="500" width="400" height="250" rx="10" ry="10" fill="#f2e6ff" stroke="#660066" stroke-width="2" opacity="0.8" />
  <text x="250" y="525" font-family="Arial" font-size="18" font-weight="bold" text-anchor="middle" fill="#660066">Caching Module</text>
  
  <!-- Layout Engine Module -->
  <rect x="550" y="500" width="400" height="250" rx="10" ry="10" fill="#fff2e6" stroke="#cc6600" stroke-width="2" opacity="0.8" stroke-dasharray="5,5" />
  <text x="750" y="525" font-family="Arial" font-size="18" font-weight="bold" text-anchor="middle" fill="#cc6600">Layout Engine Module (Future)</text>
  <text x="750" y="550" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600" font-style="italic">Placeholder - Planned for Future Implementation</text>
  
  <!-- Style Computation Module Components -->
  <rect x="70" y="230" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="155" y="260" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">StyleComputationEngine</text>
  
  <rect x="260" y="230" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="345" y="260" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">StyleSheetManager</text>
  
  <rect x="70" y="300" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="155" y="330" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">SelectorMatcher</text>
  
  <rect x="260" y="300" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="345" y="330" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">CascadeResolver</text>
  
  <rect x="70" y="370" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="155" y="400" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">InheritanceProcessor</text>
  
  <rect x="260" y="370" width="170" height="50" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="345" y="400" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">ValueComputer</text>
  
  <!-- Document Lifecycle Module Components -->
  <rect x="570" y="230" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="655" y="260" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">DocumentLifecycleManager</text>
  
  <rect x="760" y="230" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="845" y="260" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">MutationObserverAdapter</text>
  
  <rect x="570" y="300" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="655" y="330" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">InvalidationManager</text>
  
  <rect x="760" y="300" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="845" y="330" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">StyleInvalidationTracker</text>
  
  <rect x="570" y="370" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="655" y="400" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">LayoutInvalidationTracker</text>
  
  <rect x="760" y="370" width="170" height="50" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="845" y="400" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">SchedulingService</text>
  
  <!-- Caching Module Components -->
  <rect x="70" y="550" width="170" height="50" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="155" y="580" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">LayoutEngineCacheManager</text>
  
  <rect x="260" y="550" width="170" height="50" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="345" y="580" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">StyleCache</text>
  
  <rect x="70" y="620" width="170" height="50" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="155" y="650" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">LayoutBoxCache</text>
  
  <rect x="260" y="620" width="170" height="50" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="345" y="650" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">CacheDependencyTracker</text>
  
  <rect x="165" y="690" width="170" height="50" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="250" y="720" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">EnhancedDependencyTracker</text>
  
  <!-- Layout Engine Module Components (Future) -->
  <rect x="570" y="590" width="170" height="50" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" stroke-dasharray="5,5" />
  <text x="655" y="620" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">LayoutEngine</text>
  
  <rect x="760" y="590" width="170" height="50" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" stroke-dasharray="5,5" />
  <text x="845" y="620" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">BoxModelComputer</text>
  
  <rect x="570" y="660" width="170" height="50" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" stroke-dasharray="5,5" />
  <text x="655" y="690" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">FlexLayoutComputer</text>
  
  <rect x="760" y="660" width="170" height="50" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" stroke-dasharray="5,5" />
  <text x="845" y="690" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">GridLayoutComputer</text>
  
  <!-- Connections between AngleSharp Core and Modules -->
  <path d="M500,140 L250,180" stroke="#000033" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  <path d="M500,140 L750,180" stroke="#000033" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Connections between Style Computation and Document Lifecycle Modules -->
  <path d="M450,320 L570,320" stroke="#004C99" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  <text x="510" y="310" font-family="Arial" font-size="12" fill="#004C99">Style Updates</text>
  
  <path d="M570,280 L450,280" stroke="#004D00" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  <text x="510" y="270" font-family="Arial" font-size="12" fill="#004D00">Invalidation</text>
  
  <!-- Connections between Modules and Caching -->
  <path d="M250,460 L250,500" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  <text x="260" y="480" font-family="Arial" font-size="12" fill="#0066cc">Cache Updates</text>
  
  <path d="M750,460 L420,550" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  <text x="600" y="490" font-family="Arial" font-size="12" fill="#006600">Cache Invalidation</text>
  
  <!-- Connections between Caching and Layout Engine -->
  <path d="M320,620 L570,620" stroke="#660066" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  <text x="450" y="610" font-family="Arial" font-size="12" fill="#660066">Layout Caching</text>
  
  <!-- Connections between Style Computation and Layout Engine -->
  <path d="M400,420 L600,590" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  <text x="450" y="520" font-family="Arial" font-size="12" fill="#0066cc">Computed Styles</text>
  
  <!-- Connections between Document Lifecycle and Layout Engine -->
  <path d="M655,420 L655,590" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  <text x="670" y="500" font-family="Arial" font-size="12" fill="#006600">Layout Invalidation</text>
  
  <!-- User Interface Layer (future) -->
  <rect x="250" y="770" width="500" height="20" rx="5" ry="5" fill="#e6e6e6" stroke="#333333" stroke-width="2" stroke-dasharray="5,5" />
  <text x="500" y="785" font-family="Arial" font-size="14" text-anchor="middle" fill="#333333">Rendering Layer (Future)</text>
  
  <!-- Legend -->
  <rect x="840" y="690" width="150" height="100" rx="5" ry="5" fill="white" stroke="#333333" stroke-width="1" />
  <text x="915" y="710" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#333333">Legend</text>
  
  <rect x="850" y="720" width="15" height="15" fill="#e6f3ff" stroke="#0066cc" stroke-width="1" />
  <text x="875" y="733" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Style Computation</text>
  
  <rect x="850" y="740" width="15" height="15" fill="#e6ffe6" stroke="#006600" stroke-width="1" />
  <text x="875" y="753" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Document Lifecycle</text>
  
  <rect x="850" y="760" width="15" height="15" fill="#f2e6ff" stroke="#660066" stroke-width="1" />
  <text x="875" y="773" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Caching</text>
  
  <rect x="850" y="780" width="15" height="15" fill="#fff2e6" stroke="#cc6600" stroke-width="1" stroke-dasharray="2,2" />
  <text x="875" y="793" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Layout (Future)</text>
  
  <!-- Arrow Definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#333333" />
    </marker>
  </defs>
</svg>