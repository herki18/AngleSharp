<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 600">
  <!-- Background -->
  <rect width="800" height="600" fill="#f8f9fa" />
  
  <!-- Title -->
  <text x="400" y="30" font-family="Arial" font-size="20" text-anchor="middle" font-weight="bold">AngleSharp.LayoutEngine Caching Architecture</text>
  
  <!-- Cache Manager -->
  <rect x="300" y="60" width="200" height="60" rx="5" fill="#4285f4" stroke="#2c5bb4" stroke-width="2" />
  <text x="400" y="95" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">LayoutEngineCacheManager</text>
  
  <!-- Dependency Tracker -->
  <rect x="300" y="150" width="200" height="60" rx="5" fill="#0f9d58" stroke="#0b6e3e" stroke-width="2" />
  <text x="400" y="185" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">CacheDependencyTracker</text>
  
  <!-- Connections between Manager and Tracker -->
  <line x1="400" y1="120" x2="400" y2="150" stroke="#666" stroke-width="2" />
  
  <!-- Cache Types -->
  <rect x="100" y="250" width="180" height="50" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="190" y="280" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">StyleCache</text>
  
  <rect x="520" y="250" width="180" height="50" rx="5" fill="#db4437" stroke="#a52714" stroke-width="2" />
  <text x="610" y="280" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">LayoutBoxCache&lt;T&gt;</text>
  
  <!-- Connections between Manager and Cache Types -->
  <line x1="330" y1="120" x2="190" y2="250" stroke="#666" stroke-width="2" />
  <line x1="470" y1="120" x2="610" y2="250" stroke="#666" stroke-width="2" />
  
  <!-- Connections between Tracker and Cache Types -->
  <line x1="330" y1="180" x2="190" y2="250" stroke="#666" stroke-width="2" stroke-dasharray="5,5" />
  <line x1="470" y1="180" x2="610" y2="250" stroke="#666" stroke-width="2" stroke-dasharray="5,5" />
  
  <!-- Base Cache Implementation -->
  <rect x="300" y="350" width="200" height="50" rx="5" fill="#f4b400" stroke="#e09d0c" stroke-width="2" />
  <text x="400" y="380" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">LayoutEngineCache&lt;TKey,TValue&gt;</text>
  
  <!-- Connections between Concrete and Base -->
  <line x1="190" y1="300" x2="340" y2="350" stroke="#666" stroke-width="2" />
  <line x1="610" y1="300" x2="460" y2="350" stroke="#666" stroke-width="2" />
  
  <!-- Cache Interface -->
  <rect x="300" y="450" width="200" height="50" rx="5" fill="#9e9e9e" stroke="#616161" stroke-width="2" />
  <text x="400" y="480" font-family="Arial" font-size="14" fill="white" text-anchor="middle" font-weight="bold">IComputationCache&lt;TKey,TValue&gt;</text>
  
  <!-- Connection between Base and Interface -->
  <line x1="400" y1="400" x2="400" y2="450" stroke="#666" stroke-width="2" />
  
  <!-- Cache Keys -->
  <rect x="100" y="350" width="130" height="40" rx="5" fill="#9c27b0" stroke="#7b1fa2" stroke-width="2" />
  <text x="165" y="375" font-family="Arial" font-size="12" fill="white" text-anchor="middle" font-weight="bold">StyleCacheKey</text>
  
  <rect x="570" y="350" width="130" height="40" rx="5" fill="#9c27b0" stroke="#7b1fa2" stroke-width="2" />
  <text x="635" y="375" font-family="Arial" font-size="12" fill="white" text-anchor="middle" font-weight="bold">LayoutCacheKey</text>
  
  <!-- VersionedKey -->
  <rect x="300" y="520" width="200" height="40" rx="5" fill="#9c27b0" stroke="#7b1fa2" stroke-width="2" />
  <text x="400" y="545" font-family="Arial" font-size="12" fill="white" text-anchor="middle" font-weight="bold">VersionedKey&lt;T&gt;</text>
  
  <!-- Connection between Interface and VersionedKey -->
  <line x1="400" y1="500" x2="400" y2="520" stroke="#666" stroke-width="2" stroke-dasharray="5,5" />
  
  <!-- Legend -->
  <rect x="600" y="500" width="15" height="15" fill="#4285f4" />
  <text x="620" y="513" font-family="Arial" font-size="12" fill="#333">Manager</text>
  
  <rect x="600" y="520" width="15" height="15" fill="#0f9d58" />
  <text x="620" y="533" font-family="Arial" font-size="12" fill="#333">Tracker</text>
  
  <rect x="600" y="540" width="15" height="15" fill="#db4437" />
  <text x="620" y="553" font-family="Arial" font-size="12" fill="#333">Specialized Caches</text>
  
  <rect x="600" y="560" width="15" height="15" fill="#f4b400" />
  <text x="620" y="573" font-family="Arial" font-size="12" fill="#333">Base Classes</text>
  
  <rect x="600" y="580" width="15" height="15" fill="#9e9e9e" />
  <text x="620" y="593" font-family="Arial" font-size="12" fill="#333">Interfaces</text>
  
  <rect x="700" y="500" width="15" height="15" fill="#9c27b0" />
  <text x="720" y="513" font-family="Arial" font-size="12" fill="#333">Keys</text>
  
  <line x1="700" y1="525" x2="730" y2="525" stroke="#666" stroke-width="2" />
  <text x="720" y="533" font-family="Arial" font-size="12" fill="#333">Inheritance</text>
  
  <line x1="700" y1="545" x2="730" y2="545" stroke="#666" stroke-width="2" stroke-dasharray="5,5" />
  <text x="720" y="553" font-family="Arial" font-size="12" fill="#333">Dependency</text>
</svg>