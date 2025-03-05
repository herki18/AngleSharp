<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 650">
  <!-- Background and border -->
  <rect width="900" height="650" fill="#f8f9fa" rx="10" ry="10" stroke="#d0d7de" stroke-width="2"/>
  
  <!-- Title -->
  <text x="450" y="40" font-family="Arial, sans-serif" font-size="24" text-anchor="middle" font-weight="bold" fill="#24292e">DOM Mutation Processing Flow</text>
  
  <!-- Flow Diagram -->
  <!-- Start -->
  <circle cx="450" cy="90" r="25" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="450" y="95" font-family="Arial, sans-serif" font-size="14" text-anchor="middle" fill="#004085">Start</text>
  
  <!-- DOM Mutation -->
  <rect x="350" y="140" width="200" height="60" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="450" y="165" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">DOM Mutation Occurs</text>
  <text x="450" y="185" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">Element added, removed, or modified</text>
  
  <!-- MutationObserver Callback -->
  <rect x="350" y="230" width="200" height="60" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="450" y="255" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">MutationObserver Callback</text>
  <text x="450" y="275" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">MutationObserverAdapter.OnMutation()</text>
  
  <!-- Batch Collection -->
  <rect x="350" y="320" width="200" height="60" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="450" y="345" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">Mutation Batch Collection</text>
  <text x="450" y="365" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">MutationBatchProcessor.AddMutation()</text>
  
  <!-- Batch Optimization -->
  <rect x="350" y="410" width="200" height="60" rx="5" ry="5" fill="#fff3cd" stroke="#856404" stroke-width="2"/>
  <text x="450" y="435" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#856404">Mutation Optimization</text>
  <text x="450" y="455" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#856404">Remove redundancies, order mutations</text>
  
  <!-- Decision: More mutations? -->
  <polygon points="450,500 490,530 450,560 410,530" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="450" y="525" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">More</text>
  <text x="450" y="540" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">mutations?</text>
  
  <!-- Dependency Analysis -->
  <rect x="50" y="500" width="200" height="60" rx="5" ry="5" fill="#f8d7da" stroke="#721c24" stroke-width="2"/>
  <text x="150" y="525" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#721c24">Dependency Analysis</text>
  <text x="150" y="545" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#721c24">Find affected elements and properties</text>
  
  <!-- Invalidation Determination -->
  <rect x="50" y="410" width="200" height="60" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2"/>
  <text x="150" y="435" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">Invalidation Determination</text>
  <text x="150" y="455" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">What needs to be recalculated?</text>
  
  <!-- Mark Elements Dirty -->
  <rect x="50" y="320" width="200" height="60" rx="5" ry="5" fill="#d4edda" stroke="#155724" stroke-width="2"/>
  <text x="150" y="345" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#155724">Mark Elements Dirty</text>
  <text x="150" y="365" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#155724">StyleInvalidationTracker.Invalidate()</text>
  
  <!-- Lifecycle State Update -->
  <rect x="50" y="230" width="200" height="60" rx="5" ry="5" fill="#d1ecf1" stroke="#0c5460" stroke-width="2"/>
  <text x="150" y="255" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#0c5460">Lifecycle State Update</text>
  <text x="150" y="275" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#0c5460">DocumentLifecycleManager.TransitionTo()</text>
  
  <!-- Schedule Update -->
  <rect x="50" y="140" width="200" height="60" rx="5" ry="5" fill="#e2e3e5" stroke="#383d41" stroke-width="2"/>
  <text x="150" y="165" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#383d41">Schedule Update</text>
  <text x="150" y="185" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#383d41">UpdateScheduler.ScheduleUpdate()</text>
  
  <!-- Update Processing -->
  <rect x="650" y="140" width="200" height="60" rx="5" ry="5" fill="#e2e3e5" stroke="#383d41" stroke-width="2"/>
  <text x="750" y="165" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#383d41">Process Updates</text>
  <text x="750" y="185" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#383d41">UpdateScheduler.ProcessPendingUpdates()</text>
  
  <!-- Style Recalculation -->
  <rect x="650" y="230" width="200" height="60" rx="5" ry="5" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="750" y="255" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#004085">Style Recalculation</text>
  <text x="750" y="275" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">StyleComputationEngine.ComputeElementStyle()</text>
  
  <!-- Decision: Need layout update? -->
  <polygon points="750,320 790,350 750,380 710,350" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="750" y="345" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">Need layout</text>
  <text x="750" y="360" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">update?</text>
  
  <!-- Layout Calculation -->
  <rect x="650" y="410" width="200" height="60" rx="5" ry="5" fill="#cce5ff" stroke="#004085" stroke-width="2" stroke-dasharray="5,5"/>
  <text x="750" y="435" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#004085">Layout Calculation</text>
  <text x="750" y="455" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#004085">(Future) LayoutEngine.CalculateLayout()</text>
  
  <!-- Lifecycle State Update (Clean) -->
  <rect x="650" y="500" width="200" height="60" rx="5" ry="5" fill="#d1ecf1" stroke="#0c5460" stroke-width="2"/>
  <text x="750" y="525" font-family="Arial, sans-serif" font-size="16" font-weight="bold" text-anchor="middle" fill="#0c5460">Update Lifecycle State</text>
  <text x="750" y="545" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#0c5460">TransitionTo(LifecycleState.Clean)</text>
  
  <!-- End -->
  <circle cx="750" cy="590" r="25" fill="#cce5ff" stroke="#004085" stroke-width="2"/>
  <text x="750" y="595" font-family="Arial, sans-serif" font-size="14" text-anchor="middle" fill="#004085">End</text>
  
  <!-- Connection Lines -->
  <!-- Main flow -->
  <line x1="450" y1="115" x2="450" y2="140" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="450" y1="200" x2="450" y2="230" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="450" y1="290" x2="450" y2="320" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="450" y1="380" x2="450" y2="410" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="450" y1="470" x2="450" y2="500" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- Decision flow - more mutations -->
  <line x1="410" y1="530" x2="350" y2="530" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <text x="380" y="520" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#24292e">No</text>
  
  <line x1="490" y1="530" x2="550" y2="530" x2="550" y2="350" x2="500" y2="350" stroke="#24292e" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <text x="520" y="520" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#24292e">Yes</text>
  
  <!-- Invalidation flow -->
  <line x1="250" y1="530" x2="300" y2="530" x2="300" y2="440" x2="250" y2="440" stroke="#24292e" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <line x1="150" y1="470" x2="150" y2="380" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="150" y1="320" x2="150" y2="290" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="150" y1="230" x2="150" y2="200" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="150" y1="140" x2="150" y2="110" x2="300" y2="110" x2="300" y2="550" x2="650" y2="550" stroke="#24292e" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  
  <!-- Update processing flow -->
  <line x1="750" y1="200" x2="750" y2="230" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="750" y1="290" x2="750" y2="320" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- Layout decision flow -->
  <line x1="710" y1="350" x2="650" y2="350" x2="650" y2="500" x2="700" y2="500" stroke="#24292e" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <text x="680" y="340" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#24292e">No</text>
  
  <line x1="790" y1="350" x2="850" y2="350" x2="850" y2="440" x2="800" y2="440" stroke="#24292e" stroke-width="2" fill="none" marker-end="url(#arrowhead)"/>
  <text x="820" y="340" font-family="Arial, sans-serif" font-size="12" text-anchor="middle" fill="#24292e">Yes</text>
  
  <line x1="750" y1="470" x2="750" y2="500" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  <line x1="750" y1="560" x2="750" y2="565" stroke="#24292e" stroke-width="2" marker-end="url(#arrowhead)"/>
  
  <!-- Arrows and marker definitions -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#24292e"/>
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="100" y="600" width="15" height="15" fill="#fff3cd" stroke="#856404" stroke-width="1"/>
  <text x="125" y="613" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Mutation Handling</text>
  
  <rect x="300" y="600" width="15" height="15" fill="#d4edda" stroke="#155724" stroke-width="1"/>
  <text x="325" y="613" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Invalidation</text>
  
  <rect x="500" y="600" width="15" height="15" fill="#d1ecf1" stroke="#0c5460" stroke-width="1"/>
  <text x="525" y="613" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Lifecycle Management</text>
  
  <rect x="700" y="600" width="15" height="15" fill="#e2e3e5" stroke="#383d41" stroke-width="1"/>
  <text x="725" y="613" font-family="Arial, sans-serif" font-size="12" fill="#24292e">Update Processing</text>
</svg>