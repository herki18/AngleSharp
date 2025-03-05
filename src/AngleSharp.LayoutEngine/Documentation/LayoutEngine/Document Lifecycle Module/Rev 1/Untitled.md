<?xml version="1.0" encoding="UTF-8"?>
<svg viewBox="0 0 900 750" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="900" height="750" fill="#f8f9fa" />
  
  <!-- Module Boundaries -->
  <rect x="50" y="40" width="800" height="180" rx="10" ry="10" fill="#e6f3ff" stroke="#0066cc" stroke-width="2" opacity="0.8" />
  <rect x="50" y="240" width="800" height="240" rx="10" ry="10" fill="#e6ffe6" stroke="#006600" stroke-width="2" opacity="0.8" />
  <rect x="50" y="500" width="800" height="130" rx="10" ry="10" fill="#fff2e6" stroke="#cc6600" stroke-width="2" opacity="0.8" />
  <rect x="50" y="650" width="800" height="80" rx="10" ry="10" fill="#f2e6ff" stroke="#660066" stroke-width="2" opacity="0.8" />
  
  <!-- Module Titles -->
  <text x="70" y="65" font-family="Arial" font-size="18" font-weight="bold" fill="#0066cc">DOM Observation and Mutation Module</text>
  <text x="70" y="265" font-family="Arial" font-size="18" font-weight="bold" fill="#006600">Invalidation Management Module</text>
  <text x="70" y="525" font-family="Arial" font-size="18" font-weight="bold" fill="#cc6600">Existing Style and Layout Computation Modules</text>
  <text x="70" y="675" font-family="Arial" font-size="18" font-weight="bold" fill="#660066">Caching Module</text>
  
  <!-- Components in DOM Observation Module -->
  <rect x="80" y="80" width="180" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="170" y="115" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">MutationObserverAdapter</text>
  
  <rect x="290" y="80" width="180" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="380" y="115" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">MutationBatchProcessor</text>
  
  <rect x="500" y="80" width="180" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="590" y="115" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">DocumentLifecycleManager</text>
  
  <rect x="500" y="150" width="180" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="590" y="185" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">SchedulingService</text>
  
  <!-- Components in Invalidation Module -->
  <rect x="80" y="290" width="180" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="170" y="325" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">InvalidationManager</text>
  
  <rect x="290" y="290" width="180" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="380" y="325" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">StyleInvalidationTracker</text>
  
  <rect x="500" y="290" width="180" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="590" y="325" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">LayoutInvalidationTracker</text>
  
  <rect x="290" y="380" width="180" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="380" y="415" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">EnhancedDependencyTracker</text>
  
  <!-- Components in Computation Module -->
  <rect x="80" y="550" width="180" height="60" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" />
  <text x="170" y="585" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">StyleComputationEngine</text>
  
  <rect x="290" y="550" width="180" height="60" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" />
  <text x="380" y="585" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">LayoutEngine (Future)</text>
  
  <!-- Components in Cache Module -->
  <rect x="80" y="660" width="180" height="60" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="170" y="695" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">StyleCache</text>
  
  <rect x="290" y="660" width="180" height="60" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="380" y="695" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">LayoutCache</text>
  
  <rect x="500" y="660" width="180" height="60" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="590" y="695" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">CacheDependencyTracker</text>
  
  <!-- DOM Connection -->
  <rect x="80" y="10" width="180" height="40" rx="5" ry="5" fill="#ffffff" stroke="#333333" stroke-width="2" />
  <text x="170" y="35" font-family="Arial" font-size="14" text-anchor="middle" fill="#333333">AngleSharp DOM</text>
  
  <!-- External Components -->
  <rect x="710" y="80" width="120" height="60" rx="5" ry="5" fill="#ffffff" stroke="#333333" stroke-width="2" />
  <text x="770" y="115" font-family="Arial" font-size="14" text-anchor="middle" fill="#333333">MutationObserver</text>
  
  <!-- Flow Arrows -->
  <!-- DOM to Adapter -->
  <path d="M170,50 L170,80" stroke="#333333" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- MutationObserver to Adapter -->
  <path d="M710,110 L260,110" stroke="#333333" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Adapter to Batch Processor -->
  <path d="M260,110 L290,110" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Batch Processor to Invalidation Manager -->
  <path d="M380,140 L380,290" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Invalidation Manager to Trackers -->
  <path d="M260,320 L290,320" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M260,320 L500,320" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Invalidation Manager to Dependency Tracker -->
  <path d="M170,350 L170,410 L290,410" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Invalidation Manager to Lifecycle Manager -->
  <path d="M170,290 L170,250 L590,250 L590,210" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Lifecycle Manager to Scheduling Service -->
  <path d="M590,140 L590,150" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Scheduling Service to Computation Engine -->
  <path d="M510,180 L450,180 L450,550" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M450,550 L380,550" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M450,550 L170,550" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Computation Engines to Caches -->
  <path d="M170,610 L170,660" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M380,610 L380,660" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Dependency Tracker to Cache Dependency Tracker -->
  <path d="M470,410 L590,410 L590,660" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Invalidation Manager to Caches -->
  <path d="M60,320 L40,320 L40,690 L80,690" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M40,690 L290,690" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Arrow Definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#333333" />
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="690" y="380" width="150" height="120" rx="5" ry="5" fill="white" stroke="#333333" stroke-width="1" />
  <text x="765" y="400" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#333333">Legend</text>
  
  <rect x="700" y="410" width="20" height="20" fill="#e6f3ff" stroke="#0066cc" stroke-width="1" />
  <text x="730" y="425" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">DOM Observation</text>
  
  <rect x="700" y="435" width="20" height="20" fill="#e6ffe6" stroke="#006600" stroke-width="1" />
  <text x="730" y="450" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Invalidation</text>
  
  <rect x="700" y="460" width="20" height="20" fill="#fff2e6" stroke="#cc6600" stroke-width="1" />
  <text x="730" y="475" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Computation</text>
  
  <rect x="700" y="485" width="20" height="20" fill="#f2e6ff" stroke="#660066" stroke-width="1" />
  <text x="730" y="500" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Caching</text>
</svg>