#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace AngleSharp.Browser.Dom;

using System;

public class Navigator : INavigator
{
    public String Name { get; } = "Unity";
    public String Version { get; } = "1.0";
    public String Platform { get; } = "Unity";
    public String UserAgent { get; } = "Unity";

    public void RegisterProtocolHandler(string scheme, string url, string title)
    {
        throw new NotImplementedException();
    }

    public void RegisterContentHandler(string mimeType, string url, string title)
    {
        throw new NotImplementedException();
    }

    public Boolean IsProtocolHandlerRegistered(string scheme, string url)
    {
        throw new NotImplementedException();
    }

    public Boolean IsContentHandlerRegistered(string mimeType, string url)
    {
        throw new NotImplementedException();
    }

    public void UnregisterProtocolHandler(string scheme, string url)
    {
        throw new NotImplementedException();
    }

    public void UnregisterContentHandler(string mimeType, string url)
    {
        throw new NotImplementedException();
    }

    public void WaitForStorageUpdates()
    {
        throw new NotImplementedException();
    }

    public bool IsOnline { get; }
}