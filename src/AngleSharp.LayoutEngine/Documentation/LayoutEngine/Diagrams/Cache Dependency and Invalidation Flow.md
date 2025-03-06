<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 600">
  <!-- Background -->
  <rect width="800" height="600" fill="#f8f9fa" />
  
  <!-- Title -->
  <text x="400" y="30" font-family="Arial" font-size="20" text-anchor="middle" font-weight="bold">Cache Dependency and Invalidation Flow</text>
  
  <!-- DOM Tree -->
  <rect x="50" y="80" width="150" height="440" rx="5" fill="#e0e0e0" stroke="#9e9e9e" stroke-width="2" />
  <text x="125" y="110" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold">DOM Tree</text>
  
  <!-- Root Element -->
  <rect x="70" y="130" width="110" height="40" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="125" y="155" font-family="Arial" font-size="12" fill="white" text-anchor="middle">HTML Element</text>
  
  <!-- Body Element -->
  <rect x="70" y="190" width="110" height="40" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="125" y="215" font-family="Arial" font-size="12" fill="white" text-anchor="middle">BODY Element</text>
  
  <!-- Parent Element -->
  <rect x="70" y="250" width="110" height="40" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="125" y="275" font-family="Arial" font-size="12" fill="white" text-anchor="middle">DIV (Parent)</text>
  
  <!-- Target Element -->
  <rect x="70" y="310" width="110" height="40" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="125" y="335" font-family="Arial" font-size="12" fill="white" text-anchor="middle">DIV (Target)</text>
  
  <!-- Child Elements -->
  <rect x="70" y="370" width="110" height="40" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="125" y="395" font-family="Arial" font-size="12" fill="white" text-anchor="middle">SPAN (Child)</text>
  
  <rect x="70" y="430" width="110" height="40" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="125" y="455" font-family="Arial" font-size="12" fill="white" text-anchor="middle">P (Child)</text>
  
  <!-- DOM Tree Connections -->
  <line x1="125" y1="170" x2="125" y2="190" stroke="#666" stroke-width="1.5" />
  <line x1="125" y1="230" x2="125" y2="250" stroke="#666" stroke-width="1.5" />
  <line x1="125" y1="290" x2="125" y2="310" stroke="#666" stroke-width="1.5" />
  <line x1="125" y1="350" x2="125" y2="370" stroke="#666" stroke-width="1.5" />
  <line x1="125" y1="350" x2="125" y2="430" stroke="#666" stroke-width="1.5" />
  
  <!-- Dependency Tracker -->
  <rect x="250" y="80" width="500" height="100" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="500" y="110" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">Dependency Tracker</text>
  
  <!-- Element Dependencies -->
  <rect x="270" y="130" width="110" height="30" rx="5" fill="#81c995" stroke="#0b6e3e" stroke-width="1" />
  <text x="325" y="150" font-family="Arial" font-size="10" fill="black" text-anchor="middle">Element Dependencies</text>
  
  <!-- Style Dependencies -->
  <rect x="390" y="130" width="110" height="30" rx="5" fill="#81c995" stroke="#0b6e3e" stroke-width="1" />
  <text x="445" y="150" font-family="Arial" font-size="10" fill="black" text-anchor="middle">Style Dependencies</text>
  
  <!-- Document Dependencies -->
  <rect x="510" y="130" width="110" height="30" rx="5" fill="#81c995" stroke="#0b6e3e" stroke-width="1" />
  <text x="565" y="150" font-family="Arial" font-size="10" fill="black" text-anchor="middle">Document Dependencies</text>
  
  <!-- Layout Dependencies -->
  <rect x="630" y="130" width="100" height="30" rx="5" fill="#81c995" stroke="#0b6e3e" stroke-width="1" />
  <text x="680" y="150" font-family="Arial" font-size="10" fill="black" text-anchor="middle">Layout Dependencies</text>
  
  <!-- Style Cache -->
  <rect x="250" y="200" width="220" height="320" rx="5" fill="#eee8d5" stroke="#93a1a1" stroke-width="2" />
  <text x="360" y="230" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold">StyleCache</text>
  
  <!-- Target Element Style Cache Entry -->
  <rect x="270" y="250" width="180" height="70" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="360" y="270" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Target Element Style</text>
  <text x="360" y="290" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Key: StyleCacheKey(targetElement)</text>
  <text x="360" y="310" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Value: Computed Style Declaration</text>
  
  <!-- Parent Style Cache Entry -->
  <rect x="270" y="330" width="180" height="70" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="360" y="350" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Parent Element Style</text>
  <text x="360" y="370" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Key: StyleCacheKey(parentElement)</text>
  <text x="360" y="390" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Value: Computed Style Declaration</text>
  
  <!-- Pseudo Element Style Cache Entry -->
  <rect x="270" y="410" width="180" height="70" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="360" y="430" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Target ::before Style</text>
  <text x="360" y="450" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Key: StyleCacheKey(target, "::before")</text>
  <text x="360" y="470" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Value: Computed Style Declaration</text>
  
  <!-- Layout Cache -->
  <rect x="490" y="200" width="260" height="320" rx="5" fill="#eee8d5" stroke="#93a1a1" stroke-width="2" />
  <text x="620" y="230" font-family="Arial" font-size="14" text-anchor="middle" font-weight="bold">LayoutBoxCache&lt;BoxLayoutData&gt;</text>
  
  <!-- Target Element Layout Cache Entry -->
  <rect x="510" y="250" width="220" height="90" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="620" y="270" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Target Element Layout</text>
  <text x="620" y="290" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Key: LayoutCacheKey(targetElement, 800, 600)</text>
  <text x="620" y="310" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Value: { X: 10, Y: 20, Width: 100, Height: 50 }</text>
  <text x="620" y="330" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Version: 1</text>
  
  <!-- Child Element Layout Cache Entry -->
  <rect x="510" y="350" width="220" height="90" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="620" y="370" font-family="Arial" font-size="12" fill="white" text-anchor="middle">Child Element Layout</text>
  <text x="620" y="390" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Key: LayoutCacheKey(childElement, 100, 50)</text>
  <text x="620" y="410" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Value: { X: 5, Y: 10, Width: 90, Height: 30 }</text>
  <text x="620" y="430" font-family="Arial" font-size="10" fill="white" text-anchor="middle">Version: 1</text>
  
  <!-- Dependency Connections -->
  <!-- DOM to Dependency Tracker -->
  <line x1="180" y1="270" x2="250" y2="130" stroke="#0f9d58" stroke-width="2" stroke-dasharray="5,5" />
  <line x1="180" y1="330" x2="380" y2="130" stroke="#0f9d58" stroke-width="2" stroke-dasharray="5,5" />
  <line x1="180" y1="400" x2="510" y2="130" stroke="#0f9d58" stroke-width="2" stroke-dasharray="5,5" />
  <line x1="180" y1="450" x2="630" y2="130" stroke="#0f9d58" stroke-width="2" stroke-dasharray="5,5" />
  
  <!-- Target Element to Cache Entries -->
  <line x1="180" y1="330" x2="270" y2="280" stroke="#db4437" stroke-width="2" />
  <line x1="180" y1="330" x2="270" y2="440" stroke="#db4437" stroke-width="2" />
  <line x1="180" y1="330" x2="510" y2="280" stroke="#db4437" stroke-width="2" />
  
  <!-- Style to Layout Dependencies -->
  <line x1="450" y1="280" x2="510" y2="280" stroke="#777" stroke-width="1.5" stroke-dasharray="3,3" />
  <text x="480" y="270" font-family="Arial" font-size="10" fill="#555" text-anchor="middle">Computed Style</text>
  <text x="480" y="285" font-family="Arial" font-size="10" fill="#555" text-anchor="middle">Feeds Layout</text>
  
  <!-- Invalidation Arrows -->
  <path d="M180,330 Q220,380 240,420 T290,480" stroke="#db4437" stroke-width="3" fill="none" marker-end="url(#arrowhead)" />
  <text x="230" y="470" font-family="Arial" font-size="12" fill="#db4437" text-anchor="middle" font-weight="bold">Change in</text>
  <text x="230" y="490" font-family="Arial" font-size="12" fill="#db4437" text-anchor="middle" font-weight="bold">Target Element</text>
  
  <path d="M360,480 Q370,530 450,530 T585,480" stroke="#db4437" stroke-width="3" fill="none" marker-end="url(#arrowhead)" />
  <text x="470" y="550" font-family="Arial" font-size="12" fill="#db4437" text-anchor="middle" font-weight="bold">Invalidate Dependent Cache Entries</text>
  
  <!-- Arrow marker definition -->
  <defs>
    <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="0" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#db4437" />
    </marker>
  </defs>
  
  <!-- Legend -->
  <rect x="600" y="530" width="15" height="15" fill="#4285f4" />
  <text x="620" y="543" font-family="Arial" font-size="12" fill="#333">Standard Element</text>
  
  <rect x="600" y="550" width="15" height="15" fill="#db4437" />
  <text x="620" y="563" font-family="Arial" font-size="12" fill="#333">Target Element (Modified)</text>
  
  <rect x="600" y="570" width="15" height="15" fill="#0f9d58" />
  <text x="620" y="583" font-family="Arial" font-size="12" fill="#333">Dependency Tracking</text>
</svg>