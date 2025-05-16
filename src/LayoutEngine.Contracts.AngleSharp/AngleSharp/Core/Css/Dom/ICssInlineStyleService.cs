namespace AngleSharp.Css.Dom;

using System;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;

/// <summary>
///
/// </summary>
public interface ICssInlineStyleService
{
    /// <summary>
    ///
    /// </summary>
    ConditionalWeakTable<IElement, ICssStyleDeclarationBase> Styles { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    ICssStyleDeclarationBase CreateStyle(IElement element);

    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    ICssStyleDeclarationBase CreateStyle(IElement element, String source);

    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    ICssStyleDeclarationBase GetStyle(IElement element);

    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <param name="value"></param>
    void SetStyle(IElement element, String value);

    /// <summary>
    ///
    /// </summary>
    /// <param name="element"></param>
    /// <param name="value"></param>
    void UpdateStyle(IElement element, String value);
}