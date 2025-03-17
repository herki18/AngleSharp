namespace AngleSharp;

using System;
using System.Collections.Generic;
using Browser;
using Browser.Dom;
using Dom;

public interface IBrowsingContext : IEventTarget, IDisposable
{
    IWindow? Current { get; }
    IDocument? Active { get; set; }
    IHistory? SessionHistory { get; }
    Sandboxes Security { get; }
    IBrowsingContext? Parent { get; }
    IDocument? Creator { get; }
    IEnumerable<Object> OriginalServices { get; }
    T? GetService<T>() where T : class;
    IEnumerable<T> GetServices<T>() where T : class;
    IBrowsingContext CreateChild(String? name, Sandboxes security);
    IBrowsingContext? FindChild(String name);
}