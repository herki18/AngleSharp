namespace Run;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Text;
using AngleSharp.ViewSync;

public class UnityHtmlElementFactory : IElementFactory<Document, HtmlElement>
{
    private delegate HtmlElement Creator(Document document, String? prefix);

    private readonly Dictionary<String, Creator> _creators = new()
    {
        { TagNames.Div, (document, prefix) => new UnityHtmlDivElement(document, TagNames.Div, prefix) },
        { TagNames.Html, (document, prefix) => new UnityHtmlHtmlElement(document, TagNames.Html, prefix) },
        { TagNames.Head, (document, prefix) => new UnityHtmlHeadElement(document, TagNames.Head, prefix) },
        { TagNames.Body, (document, prefix) => new UnityHtmlBodyElement(document, TagNames.Body, prefix) },
    };

    public HtmlElement Create(Document document, String localName, String? prefix = null, NodeFlags flags = NodeFlags.None)
    {
        if (_creators.TryGetValue(localName, out var creator))
        {
            return creator.Invoke(document, prefix);
        }

        return new UnityHtmlDivElement(document, localName.HtmlLower(), prefix, flags);
    }
}

public class UnityHtmlDivElement : HtmlElement, IHtmlDivElement
{
    public UnityHtmlDivElement(Document owner, String localName, String? prefix = null, NodeFlags flags = NodeFlags.None)
        : base(owner, TagNames.Div, prefix, NodeFlags.Special)
    {
        ViewSync = new UnityViewSynchronizer(new VisualElement(), this);
    }
}

public class UnityHtmlHtmlElement : HtmlElement, IHtmlHtmlElement
{
    public UnityHtmlHtmlElement(Document owner, String localName, String? prefix = null, NodeFlags flags = NodeFlags.None)
        : base(owner, TagNames.Html, prefix, NodeFlags.Special)
    {
        ViewSync = new UnityViewSynchronizer(new VisualElement(), this);
    }

    public string? Manifest { get; set; }
}

public class UnityHtmlHeadElement : HtmlElement, IHtmlHeadElement
{
    public UnityHtmlHeadElement(Document owner, String localName, String? prefix = null, NodeFlags flags = NodeFlags.None)
        : base(owner, TagNames.Head, prefix, NodeFlags.Special)
    {
        ViewSync = new UnityViewSynchronizer(new VisualElement(), this);
    }
}

public class UnityHtmlBodyElement : HtmlElement, IHtmlBodyElement
{

        #region Events

        public event DomEventHandler Printed
        {
            add { AddEventListener(EventNames.AfterPrint, value); }
            remove { RemoveEventListener(EventNames.AfterPrint, value); }
        }

        public event DomEventHandler Printing
        {
            add { AddEventListener(EventNames.BeforePrint, value); }
            remove { RemoveEventListener(EventNames.BeforePrint, value); }
        }

        public event DomEventHandler Unloading
        {
            add { AddEventListener(EventNames.Unloading, value); }
            remove { RemoveEventListener(EventNames.Unloading, value); }
        }

        public event DomEventHandler HashChanged
        {
            add { AddEventListener(EventNames.HashChange, value); }
            remove { RemoveEventListener(EventNames.HashChange, value); }
        }

        public event DomEventHandler MessageReceived
        {
            add { AddEventListener(EventNames.Message, value); }
            remove { RemoveEventListener(EventNames.Message, value); }
        }

        public event DomEventHandler WentOffline
        {
            add { AddEventListener(EventNames.Offline, value); }
            remove { RemoveEventListener(EventNames.Offline, value); }
        }

        public event DomEventHandler WentOnline
        {
            add { AddEventListener(EventNames.Online, value); }
            remove { RemoveEventListener(EventNames.Online, value); }
        }

        public event DomEventHandler PageHidden
        {
            add { AddEventListener(EventNames.PageHide, value); }
            remove { RemoveEventListener(EventNames.PageHide, value); }
        }

        public event DomEventHandler PageShown
        {
            add { AddEventListener(EventNames.PageShow, value); }
            remove { RemoveEventListener(EventNames.PageShow, value); }
        }

        public event DomEventHandler PopState
        {
            add { AddEventListener(EventNames.PopState, value); }
            remove { RemoveEventListener(EventNames.PopState, value); }
        }

        public event DomEventHandler Storage
        {
            add { AddEventListener(EventNames.Storage, value); }
            remove { RemoveEventListener(EventNames.Storage, value); }
        }

        public event DomEventHandler Unloaded
        {
            add { AddEventListener(EventNames.Unload, value); }
            remove { RemoveEventListener(EventNames.Unload, value); }
        }

        #endregion

        #region ctor

        public UnityHtmlBodyElement(Document owner, String localName, String? prefix = null, NodeFlags flags = NodeFlags.None)
            : base(owner, TagNames.Body, prefix, NodeFlags.Special)
        {
            ViewSync = new UnityViewSynchronizer(new VisualElement(), this);
        }

        #endregion

        #region Properties

        // public String? ALink
        // {
        //     get => this.GetOwnAttribute(AttributeNames.Alink);
        //     set => this.SetOwnAttribute(AttributeNames.Alink, value);
        // }
        //
        // public String? Background
        // {
        //     get => this.GetOwnAttribute(AttributeNames.Background);
        //     set => this.SetOwnAttribute(AttributeNames.Background, value);
        // }
        //
        // public String? BgColor
        // {
        //     get => this.GetOwnAttribute(AttributeNames.BgColor);
        //     set => this.SetOwnAttribute(AttributeNames.BgColor, value);
        // }
        //
        // public String? Link
        // {
        //     get => this.GetOwnAttribute(AttributeNames.Link);
        //     set => this.SetOwnAttribute(AttributeNames.Link, value);
        // }
        //
        // public String? Text
        // {
        //     get => this.GetOwnAttribute(AttributeNames.Text);
        //     set => this.SetOwnAttribute(AttributeNames.Text, value);
        // }
        //
        // public String? VLink
        // {
        //     get => this.GetOwnAttribute(AttributeNames.Vlink);
        //     set => this.SetOwnAttribute(AttributeNames.Vlink, value);
        // }

        #endregion
}