// using System;
// using AngleSharp.Dom;
// using LayoutEngine.Contracts.Platform.Dom.Abstractions;
//
// namespace LayoutEngine.Platform.DOM.Abstractions;
//
// /// <summary>
// /// Factory for creating mutation observers.
// /// </summary>
// public class MutationObserverFactory : IMutationObserverFactory
// {
//     /// <inheritdoc />
//     public IMutationObserver Create(Action<IMutationRecord[]> callback)
//     {
//         return new MutationObserverWrapper(callback);
//     }
// }
//
// /// <summary>
// /// Wrapper for AngleSharp's MutationObserver that implements IMutationObserver.
// /// </summary>
// public class MutationObserverWrapper : IMutationObserver
// {
//     private readonly MutationObserver _observer;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="MutationObserverWrapper"/> class.
//     /// </summary>
//     /// <param name="callback">The callback to invoke when mutations occur.</param>
//     public MutationObserverWrapper(Action<IMutationRecord[]> callback)
//     {
//         // Create an AngleSharp MutationObserver with our callback
//         _observer = new MutationObserver((records, _) => callback(records));
//     }
//
//     /// <inheritdoc />
//     public void Observe(INode target, MutationObserverInit options)
//     {
//         // Use AngleSharp's Connect method with our options
//         _observer.Connect(
//             target,
//             options.Attributes,
//             options.ChildList,
//             options.CharacterData,
//             options.Subtree,
//             options.AttributeOldValue,
//             options.CharacterDataOldValue,
//             options.AttributeFilter
//         );
//     }
//
//     /// <inheritdoc />
//     public void Disconnect()
//     {
//         _observer.Disconnect();
//     }
// }