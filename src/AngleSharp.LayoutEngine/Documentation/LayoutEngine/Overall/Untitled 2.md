<?xml version="1.0" encoding="UTF-8"?>
<svg viewBox="0 0 1000 600" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="1000" height="600" fill="#f8f9fa" />
  
  <!-- Title -->
  <text x="500" y="30" font-family="Arial" font-size="24" font-weight="bold" text-anchor="middle" fill="#333333">AngleSharp Layout Engine - System Data Flow</text>
  
  <!-- Main Process Pipeline -->
  <!-- DOM and Stylesheets -->
  <rect x="100" y="100" width="150" height="60" rx="5" ry="5" fill="#e6e6ff" stroke="#000033" stroke-width="2" />
  <text x="175" y="135" font-family="Arial" font-size="14" text-anchor="middle" fill="#000033">DOM and Stylesheets</text>
  
  <!-- DOM Mutation -->
  <rect x="100" y="200" width="150" height="60" rx="5" ry="5" fill="#e6e6ff" stroke="#000033" stroke-width="2" />
  <text x="175" y="235" font-family="Arial" font-size="14" text-anchor="middle" fill="#000033">DOM Mutation</text>
  
  <!-- Mutation Detection -->
  <rect x="300" y="200" width="150" height="60" rx="5" ry="5" fill="#e6ffe6" stroke="#006600" stroke-width="2" />
  <text x="375" y="235" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">Mutation Detection</text>
  
  <!-- Invalidation Analysis -->
  <rect x="500" y="200" width="150" height="60" rx="5" ry="5" fill="#e6ffe6" stroke="#006600" stroke-width="2" />
  <text x="575" y="235" font-family="Arial" font-size="14" text-anchor="middle" fill="#006600">Invalidation Analysis</text>
  
  <!-- Cache Invalidation -->
  <rect x="700" y="200" width="150" height="60" rx="5" ry="5" fill="#f2e6ff" stroke="#660066" stroke-width="2" />
  <text x="775" y="235" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">Cache Invalidation</text>
  
  <!-- Style Recalculation -->
  <rect x="300" y="300" width="150" height="60" rx="5" ry="5" fill="#e6f3ff" stroke="#0066cc" stroke-width="2" />
  <text x="375" y="335" font-family="Arial" font-size="14" text-anchor="middle" fill="#0066cc">Style Recalculation</text>
  
  <!-- Style Cache Update -->
  <rect x="500" y="300" width="150" height="60" rx="5" ry="5" fill="#f2e6ff" stroke="#660066" stroke-width="2" />
  <text x="575" y="335" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">Style Cache Update</text>
  
  <!-- Layout Recalculation -->
  <rect x="300" y="400" width="150" height="60" rx="5" ry="5" fill="#fff2e6" stroke="#cc6600" stroke-width="2" stroke-dasharray="5,5" />
  <text x="375" y="435" font-family="Arial" font-size="14" text-anchor="middle" fill="#cc6600">Layout Recalculation</text>
  <text x="375" y="450" font-family="Arial" font-size="10" text-anchor="middle" fill="#cc6600" font-style="italic">(Future)</text>
  
  <!-- Layout Cache Update -->
  <rect x="500" y="400" width="150" height="60" rx="5" ry="5" fill="#f2e6ff" stroke="#660066" stroke-width="2" stroke-dasharray="5,5" />
  <text x="575" y="435" font-family="Arial" font-size="14" text-anchor="middle" fill="#660066">Layout Cache Update</text>
  <text x="575" y="450" font-family="Arial" font-size="10" text-anchor="middle" fill="#660066" font-style="italic">(Future)</text>
  
  <!-- Rendering -->
  <rect x="300" y="500" width="150" height="60" rx="5" ry="5" fill="#e6e6e6" stroke="#333333" stroke-width="2" stroke-dasharray="5,5" />
  <text x="375" y="535" font-family="Arial" font-size="14" text-anchor="middle" fill="#333333">Rendering</text>
  <text x="375" y="550" font-family="Arial" font-size="10" text-anchor="middle" fill="#333333" font-style="italic">(Future)</text>
  
  <!-- Flow Arrows -->
  <!-- DOM -> DOM Mutation -->
  <path d="M175,160 L175,200" stroke="#000033" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- DOM Mutation -> Mutation Detection -->
  <path d="M250,230 L300,230" stroke="#000033" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Mutation Detection -> Invalidation Analysis -->
  <path d="M450,230 L500,230" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Invalidation Analysis -> Cache Invalidation -->
  <path d="M650,230 L700,230" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Invalidation Analysis -> Style Recalculation -->
  <path d="M575,260 L575,280 L375,280 L375,300" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Style Recalculation -> Style Cache Update -->
  <path d="M450,330 L500,330" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Style Recalculation -> Layout Recalculation -->
  <path d="M375,360 L375,400" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  
  <!-- Layout Recalculation -> Layout Cache Update -->
  <path d="M450,430 L500,430" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  
  <!-- Layout Recalculation -> Rendering -->
  <path d="M375,460 L375,500" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  
  <!-- DOM -> Style Recalculation -->
  <path d="M175,160 L175,180 L80,180 L80,330 L300,330" stroke="#000033" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Feedback Loops -->
  <!-- Cache Invalidation -> Style Cache Update -->
  <path d="M775,260 L775,330 L650,330" stroke="#660066" stroke-width="2" fill="none" marker-end="url(#arrowhead)" />
  
  <!-- Cache Invalidation -> Layout Cache Update -->
  <path d="M775,260 L775,430 L650,430" stroke="#660066" stroke-width="2" fill="none" marker-end="url(#arrowhead)" stroke-dasharray="5,5" />
  
  <!-- Data Objects Section -->
  <rect x="700" y="300" width="200" height="200" rx="10" ry="10" fill="#f5f5f5" stroke="#333333" stroke-width="1" />
  <text x="800" y="320" font-family="Arial" font-size="16" font-weight="bold" text-anchor="middle" fill="#333333">Data Objects</text>
  
  <!-- MutationRecord -->
  <rect x="720" y="340" width="160" height="30" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="1" />
  <text x="800" y="360" font-family="Arial" font-size="12" text-anchor="middle" fill="#006600">MutationRecord</text>
  
  <!-- ComputedStyle -->
  <rect x="720" y="380" width="160" height="30" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="1" />
  <text x="800" y="400" font-family="Arial" font-size="12" text-anchor="middle" fill="#0066cc">ComputedStyle</text>
  
  <!-- LayoutBox -->
  <rect x="720" y="420" width="160" height="30" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="1" stroke-dasharray="3,3" />
  <text x="800" y="440" font-family="Arial" font-size="12" text-anchor="middle" fill="#cc6600">LayoutBox (Future)</text>
  
  <!-- StyleCache Entry -->
  <rect x="720" y="460" width="160" height="30" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="1" />
  <text x="800" y="480" font-family="Arial" font-size="12" text-anchor="middle" fill="#660066">StyleCache Entry</text>
  
  <!-- Legend -->
  <rect x="840" y="510" width="150" height="80" rx="5" ry="5" fill="white" stroke="#333333" stroke-width="1" />
  <text x="915" y="525" font-family="Arial" font-size="12" text-anchor="middle" font-weight="bold" fill="#333333">Legend</text>
  
  <rect x="850" y="535" width="12" height="12" fill="#e6e6ff" stroke="#000033" stroke-width="1" />
  <text x="870" y="545" font-family="Arial" font-size="10" fill="#333333" text-anchor="start">AngleSharp Core</text>
  
  <rect x="850" y="550" width="12" height="12" fill="#e6ffe6" stroke="#006600" stroke-width="1" />
  <text x="870" y="560" font-family="Arial" font-size="10" fill="#333333" text-anchor="start">Document Lifecycle</text>
  
  <rect x="850" y="565" width="12" height="12" fill="#e6f3ff" stroke="#0066cc" stroke-width="1" />
  <text x="870" y="575" font-family="Arial" font-size="10" fill="#333333" text-anchor="start">Style Computation</text>
  
  <rect x="850" y="580" width="12" height="12" fill="#f2e6ff" stroke="#660066" stroke-width="1" />
  <text x="870" y="590" font-family="Arial" font-size="10" fill="#333333" text-anchor="start">Caching</text>
  
  <!-- Arrow Definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#333333" />
    </marker>
  </defs>
  
  <!-- Components and Responsibilities Section -->
  <rect x="70" y="500" width="200" height="80" rx="5" ry="5" fill="#f5f5f5" stroke="#333333" stroke-width="1" />
  <text x="170" y="515" font-family="Arial" font-size="12" font-weight="bold" text-anchor="middle" fill="#333333">Key Components by Phase</text>
  
  <text x="95" y="535" font-family="Arial" font-size="10" fill="#006600" text-anchor="start">• MutationObserverAdapter</text>
  <text x="95" y="550" font-family="Arial" font-size="10" fill="#006600" text-anchor="start">• InvalidationManager</text>
  <text x="95" y="565" font-family="Arial" font-size="10" fill="#0066cc" text-anchor="start">• StyleComputationEngine</text>
  <text x="95" y="580" font-family="Arial" font-size="10" fill="#660066" text-anchor="start">• LayoutEngineCacheManager</text>
</svg>