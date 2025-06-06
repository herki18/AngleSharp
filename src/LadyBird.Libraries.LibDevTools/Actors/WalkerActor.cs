namespace LadyBird.Libraries.LibDevTools.Actors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

public sealed class WalkerActor : Actor
{
    public const string BaseName = "walker";

    public class DomNode
    {
        public JsonObject Node { get; set; }
        public NodeIdentifier Identifier { get; set; }
        public TabActor Tab { get; set; }
    }

    private readonly WeakReference<TabActor> _tab; // From C++ WeakPtr
    private WeakReference<LayoutInspectorActor> _layoutInspector; // From C++ WeakPtr
    private JsonObject _domTree;
    private readonly List<WebView.Mutation> _domNodeMutations = new();
    private bool _hasNewMutationsSinceLastMutationsRequest = false;

    private readonly Dictionary<JsonObject, JsonObject> _domNodeToParentMap = new();
    private readonly Dictionary<string, JsonObject> _actorToDomNodeMap = new();
    private readonly Dictionary<Web.UniqueNodeId, string> _domNodeIdToActorMap = new();
    private readonly Dictionary<NodeIdentifier, WeakReference<NodeActor>> _nodeActors = new();

    // From C++: static NonnullRefPtr<WalkerActor> create(DevToolsServer&, String name, WeakPtr<TabActor>, JsonObject dom_tree)
    public static WalkerActor Create(DevToolsServer devtools, string name, WeakReference<TabActor> tab, JsonObject domTree)
    {
        return new WalkerActor(devtools, name, tab, domTree);
    }

    private WalkerActor(DevToolsServer devtools, string name, WeakReference<TabActor> tab, JsonObject domTree)
        : base(devtools, name)
    {
        _tab = tab;
        _domTree = domTree;

        PopulateDomTreeCache();

        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (tab.TryGetTarget(out var tabActor))
        {
            var weakSelf = new WeakReference<WalkerActor>(this); // Mapping from C++ make_weak_ptr
            devtools.Delegate.ListenForDomMutations(tabActor.Description,
                (mutation) =>
                {
                    if (weakSelf.TryGetTarget(out var self))
                        self.NewDomNodeMutation(mutation);
                });
        }
    }

    ~WalkerActor()
    {
        // Using WeakReference.TryGetTarget instead of C++ weak_ptr.strong_ref()
        if (_tab.TryGetTarget(out var tab))
            Devtools.Delegate.StopListeningForDomMutations(tab.Description);
    }

    // From C++: void handle_message(Message const&)
    protected override void HandleMessage(Message message)
    {
        var response = new JsonObject();

        if (message.Type == "children")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var ancestorNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (ancestorNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            var nodes = new JsonArray();
            if (ancestorNode.Node.TryGetPropertyValue("children", out var childrenValue) &&
                childrenValue is JsonArray children)
            {
                foreach (var child in children)
                {
                    if (child is JsonObject childObj)
                        nodes.Add(SerializeNode(childObj));
                }
            }

            response["hasFirst"] = nodes.Count > 0;
            response["hasLast"] = nodes.Count > 0;
            response["nodes"] = nodes;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "duplicateNode")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.CloneDomNode(domNode.Tab.Description, domNode.Identifier.Id, DefaultAsyncHandler(message));
            return;
        }

        if (message.Type == "editTagName")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var tagNameResult = GetRequiredParameter<string>(message, "tagName");
            if (!tagNameResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.SetDomNodeTag(domNode.Tab.Description, domNode.Identifier.Id, tagNameResult.Value, DefaultAsyncHandler(message));
            return;
        }

        if (message.Type == "getLayoutInspector")
        {
            if (_layoutInspector == null || !_layoutInspector.TryGetTarget(out _))
            {
                var layoutInspector = Devtools.RegisterActor<LayoutInspectorActor>();
                _layoutInspector = new WeakReference<LayoutInspectorActor>(layoutInspector);
            }

            if (_layoutInspector.TryGetTarget(out var layoutInspectorActor))
            {
                var actor = new JsonObject
                {
                    ["actor"] = layoutInspectorActor.Name
                };
                response["actor"] = actor;
            }

            SendResponse(message, response);
            return;
        }

        if (message.Type == "getMutations")
        {
            response["mutations"] = SerializeMutations();
            SendResponse(message, response);
            _hasNewMutationsSinceLastMutationsRequest = false;
            return;
        }

        if (message.Type == "getOffsetParent")
        {
            response["node"] = JsonValue.Create((object)null);
            SendResponse(message, response);
            return;
        }

        if (message.Type == "innerHTML")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.GetDomNodeInnerHtml(domNode.Tab.Description, domNode.Identifier.Id,
                AsyncHandler<WalkerActor, string>(message, (self, html, resp) =>
                {
                    resp["value"] = html;
                }));
            return;
        }

        if (message.Type == "insertAdjacentHTML")
        {
            // FIXME: This message also contains `value` and `position` parameters, containing the HTML to insert and the
            //        location to insert it. For the "Create New Node" action, this is always "<div></div>" and "beforeEnd",
            //        which is exactly what our WebView implementation currently supports.
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.CreateChildElement(domNode.Tab.Description, domNode.Identifier.Id,
                AsyncHandler<WalkerActor, Web.UniqueNodeId>(message, (self, nodeId, resp) =>
                {
                    var nodes = new JsonArray();
                    if (self._domNodeIdToActorMap.TryGetValue(nodeId, out var actor) &&
                        self.DomNode(actor) is DomNode node)
                    {
                        nodes.Add(self.SerializeNode(node.Node));
                    }
                    resp["newParents"] = new JsonArray();
                    resp["nodes"] = nodes;
                }));
            return;
        }

        if (message.Type == "insertBefore")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var parentResult = GetRequiredParameter<string>(message, "parent");
            if (!parentResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            var parentDomNode = DomNodeFor(new WeakReference<WalkerActor>(this), parentResult.Value);
            if (parentDomNode == null)
            {
                SendUnknownActorError(message, parentResult.Value);
                return;
            }

            Web.UniqueNodeId? siblingNodeId = null;
            if (message.Data.TryGetPropertyValue("sibling", out var siblingValue) &&
                siblingValue?.GetValue<string>() is string sibling)
            {
                var siblingDomNode = DomNodeFor(new WeakReference<WalkerActor>(this), sibling);
                if (siblingDomNode == null)
                {
                    SendUnknownActorError(message, sibling);
                    return;
                }
                siblingNodeId = siblingDomNode.Identifier.Id;
            }

            Devtools.Delegate.InsertDomNodeBefore(domNode.Tab.Description, domNode.Identifier.Id,
                parentDomNode.Identifier.Id, siblingNodeId, DefaultAsyncHandler(message));
            return;
        }

        if (message.Type == "isInDOMTree")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            response["attached"] = _actorToDomNodeMap.ContainsKey(nodeResult.Value);
            SendResponse(message, response);
            return;
        }

        if (message.Type == "outerHTML")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.GetDomNodeOuterHtml(domNode.Tab.Description, domNode.Identifier.Id,
                AsyncHandler<WalkerActor, string>(message, (self, html, resp) =>
                {
                    resp["value"] = html;
                }));
            return;
        }

        if (message.Type == "previousSibling")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            JsonValue previousSibling = JsonValue.Create((object)null);
            var previousSiblingNode = PreviousSiblingForNode(domNode.Node);
            if (previousSiblingNode != null)
                previousSibling = SerializeNode(previousSiblingNode);

            response["node"] = previousSibling;
            SendResponse(message, response);
            return;
        }

        if (message.Type == "querySelector")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var selectorResult = GetRequiredParameter<string>(message, "selector");
            if (!selectorResult.HasValue)
                return;

            var ancestorNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (ancestorNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            var selectedNode = FindNodeBySelector(ancestorNode.Node, selectorResult.Value);
            if (selectedNode != null)
            {
                response["node"] = SerializeNode(selectedNode);
                if (_domNodeToParentMap.TryGetValue(selectedNode, out var parent) &&
                    !ReferenceEquals(parent, ancestorNode.Node))
                {
                    // FIXME: Should this be a stack of nodes leading to `ancestor_node`?
                    var newParents = new JsonArray();
                    newParents.Add(SerializeNode(parent));
                    response["newParents"] = newParents;
                }
            }
            SendResponse(message, response);
            return;
        }

        if (message.Type == "removeNode")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            JsonValue nextSibling = JsonValue.Create((object)null);
            var nextSiblingNode = NextSiblingForNode(domNode.Node);
            if (nextSiblingNode != null)
                nextSibling = SerializeNode(nextSiblingNode);

            var parentNode = RemoveNode(domNode.Node);
            if (parentNode == null)
                return;

            Devtools.Delegate.RemoveDomNode(domNode.Tab.Description, domNode.Identifier.Id,
                AsyncHandler<WalkerActor, object>(message, (self, result, resp) =>
                {
                    resp["nextSibling"] = nextSibling;
                }));
            return;
        }

        if (message.Type == "retainNode")
        {
            SendResponse(message, response);
            return;
        }

        if (message.Type == "setOuterHTML")
        {
            var nodeResult = GetRequiredParameter<string>(message, "node");
            if (!nodeResult.HasValue)
                return;

            var valueResult = GetRequiredParameter<string>(message, "value");
            if (!valueResult.HasValue)
                return;

            var domNode = DomNodeFor(new WeakReference<WalkerActor>(this), nodeResult.Value);
            if (domNode == null)
            {
                SendUnknownActorError(message, nodeResult.Value);
                return;
            }

            Devtools.Delegate.SetDomNodeOuterHtml(domNode.Tab.Description, domNode.Identifier.Id,
                valueResult.Value, DefaultAsyncHandler(message));
            return;
        }

        if (message.Type == "watchRootNode")
        {
            response["type"] = "root-available";
            response["node"] = SerializeRoot();
            SendResponse(message, response);
            SendMessage(new JsonObject());
            return;
        }

        SendUnrecognizedPacketTypeError(message);
    }

    // From C++: static bool is_suitable_for_dom_inspection(JsonValue const&)
    public static bool IsSuitableForDomInspection(JsonValue node)
    {
        if (node is not JsonObject obj)
            return true;

        if (!obj.ContainsKey("name") || !obj.ContainsKey("type"))
            return false;

        if (obj.TryGetPropertyValue("text", out var textValue) &&
            textValue?.GetValue<string>() is string text &&
            string.IsNullOrWhiteSpace(text))
            return false;

        if (obj.TryGetPropertyValue("data", out var dataValue) &&
            dataValue?.GetValue<string>() is string data &&
            string.IsNullOrWhiteSpace(data))
            return false;

        return true;
    }

    // From C++: JsonValue serialize_root() const
    public JsonValue SerializeRoot()
    {
        return SerializeNode(_domTree);
    }

    // From C++: JsonValue serialize_node(JsonObject const&) const
    private JsonValue SerializeNode(JsonObject node)
    {
        if (!_tab.TryGetTarget(out var tab))
            return JsonValue.Create((object)null);

        if (!node.TryGetPropertyValue("actor", out var actorValue) ||
            actorValue?.GetValue<string>() is not string actor)
            return JsonValue.Create((object)null);

        var name = node["name"].GetValue<string>();
        var type = node["type"].GetValue<string>();
        var domType = Web.Dom.NodeType.Invalid;
        JsonValue nodeValue = JsonValue.Create((object)null);
        var isTopLevelDocument = ReferenceEquals(node, _domTree);
        var isDisplayed = !isTopLevelDocument && node.TryGetPropertyValue("visible", out var visibleValue) &&
                         visibleValue?.GetValue<bool>() == true;
        var isScrollable = node.TryGetPropertyValue("scrollable", out var scrollableValue) &&
                          scrollableValue?.GetValue<bool>() == true;
        var isShadowRoot = false;

        if (type == "document")
        {
            domType = Web.Dom.NodeType.DocumentNode;
        }
        else if (type == "element")
        {
            domType = Web.Dom.NodeType.ElementNode;
        }
        else if (type == "text")
        {
            domType = Web.Dom.NodeType.TextNode;
            if (node.TryGetPropertyValue("text", out var textValue))
                nodeValue = textValue?.GetValue<string>();
        }
        else if (type == "comment")
        {
            domType = Web.Dom.NodeType.CommentNode;
            if (node.TryGetPropertyValue("data", out var dataValue))
                nodeValue = dataValue?.GetValue<string>();
        }
        else if (type == "shadow-root")
        {
            isShadowRoot = true;
        }

        var childCount = 0;
        if (node.TryGetPropertyValue("children", out var childrenValue) && childrenValue is JsonArray children)
            childCount = children.Count;

        var attrs = new JsonArray();
        if (node.TryGetPropertyValue("attributes", out var attributesValue) && attributesValue is JsonObject attributes)
        {
            foreach (var kvp in attributes)
            {
                if (kvp.Value?.GetValue<string>() is string value)
                {
                    var attr = new JsonObject
                    {
                        ["name"] = kvp.Key,
                        ["value"] = value
                    };
                    attrs.Add(attr);
                }
            }
        }

        var serialized = new JsonObject
        {
            ["actor"] = actor,
            ["attrs"] = attrs,
            ["baseURI"] = tab.Description.Url,
            ["causesOverflow"] = false,
            ["containerType"] = JsonValue.Create((object)null),
            ["displayName"] = name.ToLowerInvariant(),
            ["displayType"] = "block",
            ["hasEventListeners"] = false,
            ["isAfterPseudoElement"] = false,
            ["isAnonymous"] = false,
            ["isBeforePseudoElement"] = false,
            ["isDirectShadowHostChild"] = JsonValue.Create((object)null),
            ["isDisplayed"] = isDisplayed,
            ["isInHTMLDocument"] = true,
            ["isMarkerPseudoElement"] = false,
            ["isNativeAnonymous"] = false,
            ["isScrollable"] = isScrollable,
            ["isShadowHost"] = false,
            ["isShadowRoot"] = isShadowRoot,
            ["isTopLevelDocument"] = isTopLevelDocument,
            ["nodeName"] = name,
            ["nodeType"] = (int)domType,
            ["nodeValue"] = nodeValue,
            ["numChildren"] = childCount,
            ["shadowRootMode"] = JsonValue.Create((object)null),
            ["traits"] = new JsonObject(),
            // FIXME: De-duplicate this string. LibDevTools currently cannot depend on LibWeb.
            ["namespaceURI"] = "http://www.w3.org/1999/xhtml"
        };

        if (!isTopLevelDocument)
        {
            if (_domNodeToParentMap.TryGetValue(node, out var parent) && parent != null)
            {
                if (parent.TryGetPropertyValue("actor", out var parentActorValue) &&
                    parentActorValue?.GetValue<string>() is string parentActor)
                {
                    serialized["parent"] = parentActor;
                }
            }
        }

        return serialized;
    }

    // From C++: static Optional<WalkerActor::DOMNode> dom_node_for(WeakPtr<WalkerActor> const&, StringView)
    public static DomNode DomNodeFor(WeakReference<WalkerActor> weakWalker, string actor)
    {
        if (weakWalker != null && weakWalker.TryGetTarget(out var walker))
            return walker.DomNode(actor);
        return null;
    }

    // From C++: Optional<DOMNode> dom_node(StringView)
    public DomNode DomNode(string actor)
    {
        if (!_tab.TryGetTarget(out var tab))
            return null;

        if (!_actorToDomNodeMap.TryGetValue(actor, out var domNode) || domNode == null)
            return null;

        var identifier = NodeIdentifier.ForNode(domNode);
        return new DomNode { Node = domNode, Identifier = identifier, Tab = tab };
    }

    // From C++: Optional<JsonObject const&> find_node_by_selector(JsonObject const&, StringView)
    private JsonObject FindNodeBySelector(JsonObject node, string selector)
    {
        bool Matches(JsonObject candidate) =>
            candidate.TryGetPropertyValue("name", out var nameValue) &&
            nameValue?.GetValue<string>()?.Equals(selector, StringComparison.OrdinalIgnoreCase) == true;

        if (Matches(node))
            return node;

        if (node.TryGetPropertyValue("children", out var childrenValue) && childrenValue is JsonArray children)
        {
            foreach (var child in children)
            {
                if (child is JsonObject childObj)
                {
                    if (Matches(childObj))
                        return childObj;
                    var result = FindNodeBySelector(childObj, selector);
                    if (result != null)
                        return result;
                }
            }
        }

        return null;
    }

    private enum Direction
    {
        Previous,
        Next
    }

    // From C++: static Optional<JsonObject const&> sibling_for_node(JsonObject const&, JsonObject const&, Direction)
    private static JsonObject SiblingForNode(JsonObject parent, JsonObject node, Direction direction)
    {
        if (!parent.TryGetPropertyValue("children", out var childrenValue) || childrenValue is not JsonArray children)
            return null;

        var index = -1;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is JsonObject child && ReferenceEquals(child, node))
            {
                index = i;
                break;
            }
        }

        if (index == -1)
            return null;

        switch (direction)
        {
            case Direction.Previous:
                if (index == 0)
                    return null;
                index--;
                break;
            case Direction.Next:
                if (index == children.Count - 1)
                    return null;
                index++;
                break;
        }

        return children[index] as JsonObject;
    }

    // From C++: Optional<JsonObject const&> previous_sibling_for_node(JsonObject const&)
    private JsonObject PreviousSiblingForNode(JsonObject node)
    {
        if (!_domNodeToParentMap.TryGetValue(node, out var parent) || parent == null)
            return null;
        return SiblingForNode(parent, node, Direction.Previous);
    }

    // From C++: Optional<JsonObject const&> next_sibling_for_node(JsonObject const&)
    private JsonObject NextSiblingForNode(JsonObject node)
    {
        if (!_domNodeToParentMap.TryGetValue(node, out var parent) || parent == null)
            return null;
        return SiblingForNode(parent, node, Direction.Next);
    }

    // From C++: Optional<JsonObject const&> remove_node(JsonObject const&)
    private JsonObject RemoveNode(JsonObject node)
    {
        if (!_domNodeToParentMap.TryGetValue(node, out var parent) || parent == null)
            return null;

        if (!parent.TryGetPropertyValue("children", out var childrenValue) || childrenValue is not JsonArray children)
            return null;

        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is JsonObject child && ReferenceEquals(child, node))
            {
                children.RemoveAt(i);
                break;
            }
        }

        PopulateDomTreeCache();
        return parent;
    }

    // From C++: void new_dom_node_mutation(WebView::Mutation)
    private void NewDomNodeMutation(WebView.Mutation mutation)
    {
        var parseResult = System.Text.Json.JsonDocument.Parse(mutation.SerializedTarget);
        if (parseResult.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            Console.WriteLine($"Unable to parse serialized target as JSON object");
            return;
        }

        var serializedTarget = JsonObject.Create(parseResult.RootElement);
        if (!ReplaceNodeInTree(serializedTarget))
        {
            Console.WriteLine("Unable to apply mutation to DOM tree");
            return;
        }

        _domNodeMutations.Add(mutation);
        if (_hasNewMutationsSinceLastMutationsRequest)
            return;

        var message = new JsonObject
        {
            ["type"] = "newMutations"
        };
        SendMessage(message);
        _hasNewMutationsSinceLastMutationsRequest = true;
    }

    // From C++: JsonValue serialize_mutations()
    private JsonValue SerializeMutations()
    {
        var mutations = new JsonArray();

        foreach (var mutation in _domNodeMutations)
        {
            if (!_domNodeIdToActorMap.TryGetValue(mutation.Target, out var target))
                continue;

            var serialized = new JsonObject
            {
                ["target"] = target,
                ["type"] = mutation.Type
            };

            mutation.MutationData.Visit(
                (WebView.AttributeMutation attrMutation) =>
                {
                    serialized["attributeName"] = attrMutation.AttributeName;
                    serialized["newValue"] = attrMutation.NewValue ?? JsonValue.Create((object)null);
                },
                (WebView.CharacterDataMutation charMutation) =>
                {
                    serialized["newValue"] = charMutation.NewValue;
                },
                (WebView.ChildListMutation childMutation) =>
                {
                    var added = new JsonArray();
                    var removed = new JsonArray();

                    foreach (var id in childMutation.Added)
                    {
                        if (_domNodeIdToActorMap.TryGetValue(id, out var nodeActor))
                            added.Add(nodeActor);
                    }

                    foreach (var id in childMutation.Removed)
                    {
                        if (_domNodeIdToActorMap.TryGetValue(id, out var nodeActor))
                            removed.Add(nodeActor);
                    }

                    serialized["added"] = added;
                    serialized["removed"] = removed;
                    serialized["numChildren"] = childMutation.TargetChildCount;
                });

            mutations.Add(serialized);
        }

        _domNodeMutations.Clear();
        return mutations;
    }

    // From C++: bool replace_node_in_tree(JsonObject)
    private bool ReplaceNodeInTree(JsonObject replacement)
    {
        var actor = ActorForNode(replacement);
        if (!_actorToDomNodeMap.TryGetValue(actor.Name, out var node) || node == null)
            return false;

        // Replace the content of the existing node with the replacement
        foreach (var prop in node.ToList())
        {
            node.Remove(prop.Key);
        }

        foreach (var prop in replacement)
        {
            node[prop.Key] = prop.Value;
        }

        PopulateDomTreeCache();
        return true;
    }

    // From C++: void populate_dom_tree_cache()
    private void PopulateDomTreeCache()
    {
        _domNodeToParentMap.Clear();
        _actorToDomNodeMap.Clear();
        _domNodeIdToActorMap.Clear();
        PopulateDomTreeCache(_domTree, null);
    }

    // From C++: void populate_dom_tree_cache(JsonObject&, JsonObject const*)
    private void PopulateDomTreeCache(JsonObject node, JsonObject parent)
    {
        var nodeActor = ActorForNode(node);
        node["actor"] = nodeActor.Name;

        _domNodeToParentMap[node] = parent;
        _actorToDomNodeMap[nodeActor.Name] = node;

        if (!nodeActor.NodeIdentifier.PseudoElement.HasValue)
            _domNodeIdToActorMap[nodeActor.NodeIdentifier.Id] = nodeActor.Name;

        if (!node.TryGetPropertyValue("children", out var childrenValue) || childrenValue is not JsonArray children)
            return;

        // Remove unsuitable nodes
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (!IsSuitableForDomInspection(children[i]))
                children.RemoveAt(i);
        }

        foreach (var child in children)
        {
            if (child is JsonObject childObj)
                PopulateDomTreeCache(childObj, node);
        }
    }

    // From C++: NodeActor const& actor_for_node(JsonObject const&)
    private NodeActor ActorForNode(JsonObject node)
    {
        var identifier = NodeIdentifier.ForNode(node);

        if (_nodeActors.TryGetValue(identifier, out var weakRef) &&
            weakRef.TryGetTarget(out var nodeActor))
        {
            return nodeActor;
        }

        nodeActor = Devtools.RegisterActor<NodeActor>(identifier, new WeakReference<WalkerActor>(this));
        _nodeActors[identifier] = new WeakReference<NodeActor>(nodeActor);
        return nodeActor;
    }
}