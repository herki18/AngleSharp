namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

public struct NodeIdentifier : IEquatable<NodeIdentifier>
{
    public Web.UniqueNodeId Id { get; set; }
    public Web.Css.PseudoElement? PseudoElement { get; set; }

    // From C++: static NodeIdentifier for_node(JsonObject const& node)
    public static NodeIdentifier ForNode(JsonObject node)
    {
        var identifier = new NodeIdentifier();

        if (node.TryGetPropertyValue("pseudo-element", out var pseudoValue) &&
            pseudoValue?.GetValue<int>() is int pseudoInt)
        {
            identifier.PseudoElement = (Web.Css.PseudoElement)pseudoInt;
        }

        if (identifier.PseudoElement.HasValue)
        {
            identifier.Id = new Web.UniqueNodeId(node["parent-id"].GetValue<ulong>());
        }
        else
        {
            identifier.Id = new Web.UniqueNodeId(node["id"].GetValue<ulong>());
        }

        return identifier;
    }

    public bool Equals(NodeIdentifier other)
    {
        return Id.Equals(other.Id) && PseudoElement == other.PseudoElement;
    }

    public override bool Equals(object obj)
    {
        return obj is NodeIdentifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, PseudoElement);
    }

    public static bool operator ==(NodeIdentifier left, NodeIdentifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NodeIdentifier left, NodeIdentifier right)
    {
        return !left.Equals(right);
    }
}

public sealed class NodeActor : Actor
{
    public const string BaseName = "node";

    private readonly NodeIdentifier _nodeIdentifier;
    private readonly WeakReference<WalkerActor> _walker; // From C++ WeakPtr

    // From C++: static NonnullRefPtr<NodeActor> create(DevToolsServer&, String name, NodeIdentifier, WeakPtr<WalkerActor>)
    public static NodeActor Create(DevToolsServer devtools, string name, NodeIdentifier nodeIdentifier, WeakReference<WalkerActor> walker)
    {
        return new NodeActor(devtools, name, nodeIdentifier, walker);
    }

    private NodeActor(DevToolsServer devtools, string name, NodeIdentifier nodeIdentifier, WeakReference<WalkerActor> walker)
        : base(devtools, name)
    {
        _nodeIdentifier = nodeIdentifier;
        _walker = walker;
    }

    public NodeIdentifier NodeIdentifier => _nodeIdentifier;
    public WeakReference<WalkerActor> Walker => _walker;

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "getUniqueSelector")
        {
            var domNode = WalkerActor.DomNodeFor(_walker, Name);
            if (domNode == null)
            {
                SendUnknownActorError(message, Name);
                return;
            }

            response["value"] = domNode.Node["name"].GetValue<string>().ToLowerInvariant();
            SendResponse(message, response);
            return;
        }

        if (message.Type == "modifyAttributes")
        {
            var modificationsResult = GetRequiredParameter<JsonArray>(message, "modifications");
            if (!modificationsResult.HasValue)
                return;

            var (attributeToReplace, replacementAttributes) = ParseAttributeModification(modificationsResult.Value);
            if (attributeToReplace == null && replacementAttributes.Count == 0)
                return;

            var domNode = WalkerActor.DomNodeFor(_walker, Name);
            if (domNode == null)
            {
                SendUnknownActorError(message, Name);
                return;
            }

            if (attributeToReplace != null)
            {
                Devtools.Delegate.ReplaceDomNodeAttribute(
                    domNode.Tab.Description,
                    domNode.Identifier.Id,
                    attributeToReplace,
                    replacementAttributes,
                    DefaultAsyncHandler(message));
            }
            else
            {
                Devtools.Delegate.AddDomNodeAttributes(
                    domNode.Tab.Description,
                    domNode.Identifier.Id,
                    replacementAttributes,
                    DefaultAsyncHandler(message));
            }
            return;
        }

        if (message.Type == "setNodeValue")
        {
            var valueResult = GetRequiredParameter<string>(message, "value");
            if (!valueResult.HasValue)
                return;

            var domNode = WalkerActor.DomNodeFor(_walker, Name);
            if (domNode == null)
            {
                SendUnknownActorError(message, Name);
                return;
            }

            Devtools.Delegate.SetDomNodeText(
                domNode.Tab.Description,
                domNode.Identifier.Id,
                valueResult.Value,
                DefaultAsyncHandler(message));
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: static AttributeModification parse_attribute_modification(JsonArray const& modifications)
    private static (string attributeToReplace, List<WebView.Attribute> replacementAttributes) ParseAttributeModification(JsonArray modifications)
    {
        if (modifications.Count == 0)
            return (null, new List<WebView.Attribute>());

        string attributeToReplace = null;
        var replacementAttributes = new List<WebView.Attribute>();

        object ParseModification(JsonNode modification)
        {
            if (modification is not JsonObject modObj)
                return null;

            if (!modObj.TryGetPropertyValue("attributeName", out var nameValue) ||
                nameValue?.GetValue<string>() is not string name)
                return null;

            if (!modObj.TryGetPropertyValue("newValue", out var valueValue) ||
                valueValue?.GetValue<string>() is not string value)
                return name;

            return new WebView.Attribute { Name = name, Value = value };
        }

        var firstMod = ParseModification(modifications[0]);
        if (firstMod == null)
            return (null, new List<WebView.Attribute>());

        if (firstMod is string name)
        {
            attributeToReplace = name;
        }
        else if (firstMod is WebView.Attribute attr)
        {
            replacementAttributes.Add(attr);
        }

        for (int i = 1; i < modifications.Count; i++)
        {
            var mod = ParseModification(modifications[i]);
            if (mod is WebView.Attribute attribute)
                replacementAttributes.Add(attribute);
        }

        return (attributeToReplace, replacementAttributes);
    }
}