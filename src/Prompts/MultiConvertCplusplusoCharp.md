Convert the following C++ files to C#.

- For each C++ file provided, generate a corresponding C# file.
- Use file-scoped namespaces in each C# file.
- Implement all methods as in the C++ code, even if they call other methods or use types that are not yet implemented.
- If a type or method is missing, create a stub for it.
- **Stub Naming and Placement:**
  - When creating stubs for missing types or methods, always use the same class, method, and namespace names as in the original C++ code.
  - For example, if the C++ code calls `Core::System::socket`, the stub should be `Core.System.Socket` in C#.
  - This ensures that the stubs can be easily replaced with real implementations later, and that the main code remains as close as possible to the original structure.
- The main implementation for each file should be as complete as possible, even if it calls stubs or contains broken code due to missing pieces.
- **Copy all comments from the C++ code into the C# code, preserving their placement and content as much as possible.**
- Add comments to indicate which methods are from C++ and where stubs are used.
- Do not worry about the correctness or completeness of the stubs—just make sure the main classes and their methods are as close to the C++ logic as possible.
- **Place all stubs (for classes, methods, or types) in a separate code block at the end of your response, under a clear separator.**
  - Group stubs by namespace, and include all stubs required by any of the input files.
- **Use C# naming conventions:**
  - Class, struct, enum, and method names should use PascalCase.
  - Property names should use PascalCase.
  - Private fields should use `_camelCase` (underscore + camelCase).
  - Local variables and method parameters should use camelCase.
  - Constants and static readonly fields should use PascalCase.
- Use C#-style access modifiers (`public`, `private`, `protected`, `internal`) as appropriate.
- Use C# types and idioms (e.g., `string` instead of `String`, `ulong` instead of `u64`).
- Prefer auto-properties for simple properties.
- Use `using` directives at the top of each file as needed.
- Use `null` instead of `nullptr`.
- Use `override` and `virtual` keywords as appropriate for inheritance.
- Use C# collection types (e.g., `List<T>`, `Dictionary<TKey, TValue>`) where applicable.
- Use `var` for local variable declarations where the type is obvious.
- Use expression-bodied members where appropriate for simple getters or methods.
- Use `nameof()` for argument names in exceptions or logging where relevant.
- **If the C++ code uses `AK::WeakPtr` or `Weakable`, map these to `WeakReference<T>` in C#:**
  - Use `WeakReference<T>` for weak references.
  - When accessing the referenced object, use `TryGetTarget(out T target)` to check if the object is still alive before using it.
  - Do not implement or require a `Weakable` base class in C#; any object can be referenced weakly.
  - If the original C++ code checks for pointer validity (e.g., `if (m_watcher)`), in C# use `TryGetTarget` or check `.IsAlive` as appropriate.
  - Add comments where this mapping occurs, explaining the conversion from `WeakPtr` to `WeakReference<T>`.
- **If the C++ code uses `ErrorOr<T>`, map it to a custom `Result<T>` class in C# with the following features:**
  - The `Result<T>` class should encapsulate either a value or an error (typically an `Exception`).
  - It should provide:  
    - `IsSuccess`/`IsFailure`  
    - `HasValue`/`HasError` (aliases for parity)  
    - `Value` and `Error` properties  
    - `TryGetValue(out T value)` and `TryGetError(out Exception error)`  
    - Static factory methods for success and failure  
    - Deconstructor and `ToString()`  
  - Use the `Result<T>` pattern for methods that return `ErrorOr<T>`.
  - **Do not provide a stub for `Result<T>`; this class is already implemented.**
- **For `JsonObject` or similar dictionary-like types, use `System.Text.Json` types (such as `System.Text.Json.Nodes.JsonObject`) and C# indexer syntax for property access.**
  - For example, convert `response.Set("type", "target-available-form");` to `response["type"] = "target-available-form";`
  - Example:
    ```csharp
    using System.Text.Json.Nodes;
    var resources = new JsonObject();
    resources["Cache"] = false;
    resources["console-message"] = true;
    ```

---

**Example Output Format:**

```csharp
// File: File1.cs
namespace MyNamespace;

// ... main implementation for File1 ...

// File: File2.cs
namespace MyNamespace;

// ... main implementation for File2 ...
```

---

```csharp
// ===== STUBS =====
// All stubs required by any file, grouped by namespace
namespace MyNamespace;

// ...
```