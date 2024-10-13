namespace Run;

using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.ViewSync;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a new browsing context
        // var config = UnityConfiguration
        //     .Default;
            // .WithCss();

        // var config = Configuration
        //     .Default
        //     .WithCss();

        var config = Configuration
            .Default
            .WithCss()
            .Without<IElementFactory<Document, HtmlElement>>()
            .With(new UnityHtmlElementFactory());

        var context = BrowsingContext.New(config);


        // HTML content to parse
        // var source = "<!DOCTYPE html><html><head><title>Example</title></head><body><div id='content'>Hello, world!</div></body></html>";

        var document = await context.OpenNewAsync();
        // Parse the HTML content
        // IDocument document = await context.OpenAsync(req => req.Content(source));
        // var linkElement = document.CreateElement(TagNames.Link);
        // linkElement.SetAttribute("rel", "stylesheet");
        // linkElement.SetAttribute("href", "https://fonts.googleapis.com/css2?family=Roboto:wght@300&display=swap");
        // document.Head?.Append(linkElement);

        document.Body.SetStyle("margin: 0; padding: 0; color: red;");

        var currentDiv = document.CreateElement(TagNames.Div);
        document.Body?.Append(currentDiv);

        // document.CreateElement<IHtmlStyleElement>();

        currentDiv.SetStyle(" font-size: 20px;");
        var style = currentDiv.GetStyle();
        Console.WriteLine("Current div Style: " + style.CssText);
        Console.WriteLine("Current Div Current Computed" + currentDiv.ComputeCurrentStyle().CssText);
        Console.WriteLine("Current Div Computed: " + currentDiv.ComputeStyle().CssText);
        var styleSheets = currentDiv.GetStyleSheets();

        // Console.WriteLine(currentDiv.GetStyleSheets());



        // Manipulate the DOM
        var div = document.QuerySelector("#content");
        if (div != null)
        {
            div.TextContent = "Hello, AngleSharp!";
        }

        // Output the modified HTML
        Console.WriteLine(document.DocumentElement.OuterHtml);
    }
}

public class VisualElement
{

}

public class UnityViewSynchronizer : IViewSynchronizer
{
    public VisualElement Element;
    public Node Node;

    public UnityViewSynchronizer(VisualElement element, Node node)
    {
        Element = element;
        Node = node;
    }
}