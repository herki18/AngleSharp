<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 600">
  <!-- Background -->
  <rect width="800" height="600" fill="#f8f9fa" />
  
  <!-- Title -->
  <text x="400" y="30" font-family="Arial" font-size="20" text-anchor="middle" font-weight="bold">Caching Integration with Style Computation Pipeline</text>
  
  <!-- Style Computation Pipeline -->
  <rect x="100" y="80" width="600" height="60" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="400" y="120" font-family="Arial" font-size="16" fill="white" text-anchor="middle" font-weight="bold">StyleComputationEngine</text>
  
  <!-- Computation Steps -->
  <rect x="100" y="170" width="100" height="50" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="150" y="200" font-family="Arial" font-size="12" fill="white" text-anchor="middle">StyleSheetManager</text>
  
  <rect x="230" y="170" width="100" height="50" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="280" y="200" font-family="Arial" font-size="12" fill="white" text-anchor="middle">SelectorMatcher</text>
  
  <rect x="360" y="170" width="100" height="50" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="410" y="200" font-family="Arial" font-size="12" fill="white" text-anchor="middle">CascadeResolver</text>
  
  <rect x="490" y="170" width="100" height="50" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="540" y="200" font-family="Arial" font-size="12" fill="white" text-anchor="middle">InheritanceProcessor</text>
  
  <rect x="620" y="170" width="100" height="50" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="670" y="200" font-family="Arial" font-size="12" fill="white" text-anchor="middle">ValueComputer</text>
  
  <!-- Connect Engine to Steps -->
  <line x1="150" y1="140" x2="150" y2="170" stroke="#0f9d58" stroke-width="1.5" />
  <line x1="280" y1="140" x2="280" y2="170" stroke="#0f9d58" stroke-width="1.5" />
  <line x1="410" y1="140" x2="410" y2="170" stroke="#0f9d58" stroke-width="1.5" />
  <line x1="540" y1="140" x2="540" y2="170" stroke="#0f9d58" stroke-width="1.5" />
  <line x1="670" y1="140" x2="670" y2="170" stroke="#0f9d58" stroke-width="1.5" />
  
  <!-- Connect Steps in Sequence -->
  <line x1="200" y1="195" x2="230" y2="195" stroke="#0f9d58" stroke-width="1.5" marker-end="url(#arrowhead-green)" />
  <line x1="330" y1="195" x2="360" y2="195" stroke="#0f9d58" stroke-width="1.5" marker-end="url(#arrowhead-green)" />
  <line x1="460" y1="195" x2="490" y2="195" stroke="#0f9d58" stroke-width="1.5" marker-end="url(#arrowhead-green)" />
  <line x1="590" y1="195" x2="620" y2="195" stroke="#0f9d58" stroke-width="1.5" marker-end="url(#arrowhead-green)" />
  
  <!-- Cache Components -->
  <rect x="230" y="250" width="340" height="60" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="400" y="290" font-family="Arial" font-size="16" fill="white" text-anchor="middle" font-weight="bold">LayoutEngineCacheManager</text>
  
  <!-- Style Cache -->
  <rect x="150" y="340" width="200" height="50" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="250" y="370" font-family="Arial" font-size="12" fill="white" text-anchor="middle">StyleCache</text>
  
  <!-- Layout Cache -->
  <rect x="450" y="340" width="200" height="50" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="550" y="370" font-family="Arial" font-size="12" fill="white" text-anchor="middle">LayoutBoxCache&lt;T&gt;</text>
  
  <!-- Connect Manager to Caches -->
  <line x1="310" y1="310" x2="250" y2="340" stroke="#db4437" stroke-width="1.5" />
  <line x1="490" y1="310" x2="550" y2="340" stroke="#db4437" stroke-width="1.5" />
  
  <!-- Dependency Tracker -->
  <rect x="300" y="420" width="200" height="50" rx="5" fill="#f4b400" stroke="#f09300" stroke-width="2" />
  <text x="400" y="450" font-family="Arial" font-size="12" fill="white" text-anchor="middle">CacheDependencyTracker</text>
  
  <!-- Connect Caches to Tracker -->
  <line x1="250" y1="390" x2="350" y2="420" stroke="#f4b400" stroke-width="1.5" stroke-dasharray="5,5" />
  <line x1="550" y1="390" x2="450" y2="420" stroke="#f4b400" stroke-width="1.5" stroke-dasharray="5,5" />
  
  <!-- Cache Check & Integration -->
  <rect x="100" y="500" width="600" height="80" rx="5" fill="#e0e0e0" stroke="#9e9e9e" stroke-width="2" />
  
  <!-- Cache Flow -->
  <!-- Check Cache First -->
  <rect x="120" y="510" width="160" height="60" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="200" y="535" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Check StyleCache</text>
  <text x="200" y="550" font-family="Arial" font-size="10" fill="white" text-anchor="middle">TryGetValue(elementKey)</text>
  
  <!-- If Miss, Compute -->
  <rect x="320" y="510" width="160" height="60" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="400" y="535" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Compute Style</text>
  <text x="400" y="550" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Execute Pipeline</text>
  
  <!-- Then Cache -->
  <rect x="520" y="510" width="160" height="60" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="600" y="535" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Cache Result</text>
  <text x="600" y="550" font-family="Arial" font-size="10" fill="white" text-anchor="middle">AddOrUpdate(elementKey, style)</text>
  
  <!-- Cache Flow Connections -->
  <line x1="280" y1="540" x2="320" y2="540" stroke="#666" stroke-width="1.5" marker-end="url(#arrowhead-black)" />
  <text x="300" y="530" font-family="Arial" font-size="10" fill="#333" text-anchor="middle">Miss</text>
  
  <line x1="480" y1="540" x2="520" y2="540" stroke="#666" stroke-width="1.5" marker-end="url(#arrowhead-black)" />
  
  <!-- Cache Hit Path -->
  <path d="M200,510 Q200,470 400,470 Q600,470 600,510" stroke="#666" stroke-width="1.5" fill="none" stroke-dasharray="5,5" marker-end="url(#arrowhead-black)" />
  <text x="400" y="460" font-family="Arial" font-size="10" fill="#333" text-anchor="middle">Hit: Return Cached Value</text>
  
  <!-- Connect Engine to Cache Check -->
  <line x1="400" y1="140" x2="400" y2="250" stroke="#db4437" stroke-width="2" stroke-dasharray="5,5" />
  <text x="420" y="180" font-family="Arial" font-size="14" fill="#db4437" font-weight="bold">Integration</text>
  
  <!-- Connect Cache Manager to Flow -->
  <line x1="400" y1="310" x2="400" y2="500" stroke="#db4437" stroke-width="1.5" marker-end="url(#arrowhead-red)" />
  
  <!-- Arrow marker definitions -->
  <defs>
    <marker id="arrowhead-black" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#666" />
    </marker>
    <marker id="arrowhead-green" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#0f9d58" />
    </marker>
    <marker id="arrowhead-red" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#db4437" />
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="30" y="550" width="15" height="15" fill="#4285f4" />
  <text x="50" y="563" font-family="Arial" font-size="10" fill="#333">Input/Check</text>
  
  <rect x="30" y="570" width="15" height="15" fill="#0f9d58" />
  <text x="50" y="583" font-family="Arial" font-size="10" fill="#333">Processing</text>
  
  <rect x="30" y="530" width="15" height="15" fill="#db4437" />
  <text x="50" y="543" font-family="Arial" font-size="10" fill="#333">Caching</text>
</svg>