namespace AngleSharp.Renderer
{
    using System.Text;
    using Css;
    using Css.RenderTree;
    using Dom;

    public class RenderEngine
    {
        private readonly IWindow _window;
        private readonly IRenderDevice _device;
        private RenderTree _builder;
        private IRenderNode? _root;


        public RenderEngine(IWindow window, IRenderDevice device)
        {
            _window = window;
            _device = device;

            _builder = new RenderTree(window, device);
        }

        public void Update()
        {
            _root = _builder.RenderDocument();

            if (_root != null)
            {
                var layoutEngine = new LayoutEngine();

                // Get device viewport or fallback to some defaults
                float viewportWidth = 800f;
                float viewportHeight = 600f;

                layoutEngine.LayoutDocument(_root, viewportWidth, viewportHeight);
            }
        }

        public IRenderNode GetRoot()
        {
            return _root!;
        }

        public string Print()
        {
            var sb = new StringBuilder();

            PrintNode(_root!, 0);

            void PrintNode(IRenderNode node, int depth)
            {
                sb.Append(new string(' ', depth * 2));
                sb.Append(node.Ref.NodeName);

                if (node.Ref is IElement element)
                {
                    var id = element.Id;
                    if(!string.IsNullOrWhiteSpace(id))
                    {
                        sb.Append(" id=");
                        sb.Append(element.Id);
                        sb.Append(" ");
                    }
                }

                if(node is ElementRenderNode elementNode)
                {
                    var css = elementNode.ComputedStyle!.ToCss();
                    if (!string.IsNullOrWhiteSpace(css))
                    {
                        sb.Append(" style=[ ");
                        sb.Append(elementNode.ComputedStyle!.ToCss());
                        sb.Append(" ]");
                    }
                }

                sb.AppendLine();

                foreach (var child in node.Children)
                {
                    PrintNode(child, depth + 1);
                }
            }

            return sb.ToString();
        }
    }

    public interface IElementWrapper
    {
        public T GetElement<T>();
    }
}