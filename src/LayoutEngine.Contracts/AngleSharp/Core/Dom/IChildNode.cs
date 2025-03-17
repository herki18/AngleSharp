namespace AngleSharp.Dom;

    using Attributes;

    /// <summary>
    ///     The ChildNode interface contains methods that are particular to Node
    ///     objects that can have a parent.
    /// </summary>
    [DomName("ChildNode")]
    [DomNoInterfaceObject]
    public interface IChildNode
    {
        [DomName("before")]
        void Before(params INode[] nodes);

        [DomName("after")]
        void After(params INode[] nodes);

        [DomName("replace")]
        void Replace(params INode[] nodes);

        [DomName("remove")]
        void Remove();
    }