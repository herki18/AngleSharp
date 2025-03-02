namespace AngleSharp.Io;

using System;
using Common;
using Dom;

/// <summary>
///     Basic contract for a currently active download.
/// </summary>
public interface IDownload : ICancellable<IResponse>
{
    /// <summary>
    ///     Gets the target of the download.
    /// </summary>
    IUrl Target { get; }

    /// <summary>
    ///     Gets the originator of the download, if any.
    /// </summary>
    Object? Source { get; }
}