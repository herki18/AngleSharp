namespace Run;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.ViewSync;

class Program
{
    static async Task Main(string[] args)
    {
        // Create a new browsing context
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);

        // HTML content to parse
        var source = "<!DOCTYPE html><html><head><title>Example</title></head><body><div id='content'>Hello, world!</div></body></html>";

        // Parse the HTML content
        var document = await context.OpenAsync(req => req.Content(source));

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