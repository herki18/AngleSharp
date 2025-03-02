namespace AngleSharp.Io;

using Attributes;

[DomNoInterfaceObject]
public interface ILoadableElement
{
    IDownload? CurrentDownload { get; }
}