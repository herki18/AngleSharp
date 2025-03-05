<svg viewBox="0 0 900 650" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="900" height="650" fill="#f8f9fa" />
  
  <!-- Title -->
  <text x="450" y="30" font-family="Arial" font-size="20" font-weight="bold" text-anchor="middle" fill="#333333">Document Lifecycle-Driven Invalidation Process Flow</text>
  
  <!-- Step Boxes -->
  <!-- 1. DOM Mutation -->
  <rect x="50" y="70" width="200" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="150" y="105" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#0066cc">1. DOM Mutation</text>
  
  <!-- 2. Mutation Observation -->
  <rect x="50" y="170" width="200" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="150" y="205" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#0066cc">2. Mutation Observation</text>
  
  <!-- 3. Mutation Batching -->
  <rect x="50" y="270" width="200" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="150" y="305" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#0066cc">3. Mutation Batching</text>
  
  <!-- 4. Invalidation Analysis -->
  <rect x="50" y="370" width="200" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="150" y="405" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#006600">4. Invalidation Analysis</text>
  
  <!-- 5. Dependency Resolution -->
  <rect x="50" y="470" width="200" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="150" y="505" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#006600">5. Dependency Resolution</text>
  
  <!-- 6. Style Invalidation -->
  <rect x="350" y="170" width="200" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="450" y="205" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#006600">6. Style Invalidation</text>
  
  <!-- 7. Layout Invalidation -->
  <rect x="350" y="270" width="200" height="60" rx="5" ry="5" fill="white" stroke="#006600" stroke-width="2" />
  <text x="450" y="305" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#006600">7. Layout Invalidation</text>
  
  <!-- 8. Cache Invalidation -->
  <rect x="350" y="370" width="200" height="60" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="450" y="405" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#660066">8. Cache Invalidation</text>
  
  <!-- 9. Lifecycle State Update -->
  <rect x="350" y="470" width="200" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="450" y="505" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#0066cc">9. Lifecycle State Update</text>
  
  <!-- 10. Update Scheduling -->
  <rect x="650" y="170" width="200" height="60" rx="5" ry="5" fill="white" stroke="#0066cc" stroke-width="2" />
  <text x="750" y="205" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#0066cc">10. Update Scheduling</text>
  
  <!-- 11. Style Recalculation -->
  <rect x="650" y="270" width="200" height="60" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" />
  <text x="750" y="305" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#cc6600">11. Style Recalculation</text>
  
  <!-- 12. Layout Recalculation -->
  <rect x="650" y="370" width="200" height="60" rx="5" ry="5" fill="white" stroke="#cc6600" stroke-width="2" />
  <text x="750" y="405" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#cc6600">12. Layout Recalculation</text>
  
  <!-- 13. Cache Update -->
  <rect x="650" y="470" width="200" height="60" rx="5" ry="5" fill="white" stroke="#660066" stroke-width="2" />
  <text x="750" y="505" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#660066">13. Cache Update</text>
  
  <!-- 14. Rendering (Future) -->
  <rect x="350" y="570" width="200" height="60" rx="5" ry="5" fill="white" stroke="#333333" stroke-width="2" stroke-dasharray="5,5" />
  <text x="450" y="605" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#333333">14. Rendering (Future)</text>
  
  <!-- Flow Arrows -->
  <!-- 1 -> 2 -->
  <path d="M150,130 L150,170" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 2 -> 3 -->
  <path d="M150,230 L150,270" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 3 -> 4 -->
  <path d="M150,330 L150,370" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 4 -> 5 -->
  <path d="M150,430 L150,470" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 5 -> 6 (Style Invalidation) -->
  <path d="M250,500 L300,500 L300,200 L350,200" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 5 -> 7 (Layout Invalidation) -->
  <path d="M250,500 L300,500 L300,300 L350,300" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 5 -> 8 (Cache Invalidation) -->
  <path d="M250,500 L300,500 L300,400 L350,400" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 6,7,8 -> 9 (Lifecycle State Update) -->
  <path d="M450,230 L450,250 L400,250 L400,470" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M450,330 L450,350 L400,350 L400,470" stroke="#006600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <path d="M450,430 L450,470" stroke="#660066" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 9 -> 10 (Update Scheduling) -->
  <path d="M550,500 L600,500 L600,200 L650,200" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 10 -> 11 (Style Recalculation) -->
  <path d="M750,230 L750,270" stroke="#0066cc" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 11 -> 12 (Layout Recalculation) -->
  <path d="M750,330 L750,370" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 12 -> 13 (Cache Update) -->
  <path d="M750,430 L750,470" stroke="#cc6600" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- 13 -> 14 (Rendering) -->
  <path d="M650,500 L600,500 L600,600 L550,600" stroke="#660066" stroke-width="2" stroke-dasharray="5,5" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Feedback Arrows -->
  <!-- 13 -> 5 (Update Dependencies) -->
  <path d="M650,500 L600,500 L600,520 L250,520" stroke="#660066" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Arrow Definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#333333" />
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="50" y="570" width="200" height="70" rx="5" ry="5" fill="white" stroke="#333333" stroke-width="1" />
  <text x="150" y="590" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold" fill="#333333">Color Legend</text>
  
  <rect x="70" y="600" width="20" height="10" fill="#0066cc" />
  <text x="105" y="610" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Observation</text>
  
  <rect x="150" y="600" width="20" height="10" fill="#006600" />
  <text x="185" y="610" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Invalidation</text>
  
  <rect x="70" y="620" width="20" height="10" fill="#cc6600" />
  <text x="105" y="630" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Computation</text>
  
  <rect x="150" y="620" width="20" height="10" fill="#660066" />
  <text x="185" y="630" font-family="Arial" font-size="12" fill="#333333" text-anchor="start">Caching</text>
</svg>