namespace AngleSharp.Browser.Dom;

using Attributes;

[DomName("Navigator")]
public interface INavigator : INavigatorId, INavigatorContentUtilities, INavigatorStorageUtilities, INavigatorOnline
{
}