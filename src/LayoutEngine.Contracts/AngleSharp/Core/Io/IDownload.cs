namespace AngleSharp.Io;

using System;
using Common;
using Dom;

/// <summary>
///     Basic contract for a currently active download.
/// </summary>
public interface IDownload : ICancellable<IResponse>
{
    IUrl Target { get; }
    Object? Source { get; }
}