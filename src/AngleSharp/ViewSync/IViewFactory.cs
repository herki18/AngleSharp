namespace AngleSharp.ViewSync;

using System;
using Dom;

/// <summary>
///
/// </summary>
public interface IViewFactory
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="localName"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    IViewSynchronizer Create(String localName, INode node);
}