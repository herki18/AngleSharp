// namespace LayoutEngine.Platform.Tests.Helpers;
//
// using System;
// using Contracts.Platform.Dom;
// using Contracts.Platform.Dom.Abstractions;
// using Unit.Helpers;
//
// public class TestMutationObserverFactory : IMutationObserverFactory
// {
//     public IMutationObserver Create(Action<MutationRecord[]> callback)
//     {
//         if (callback == null)
//             throw new ArgumentNullException(nameof(callback));
//
//         return new TestMutationObserver(callback);
//     }
// }