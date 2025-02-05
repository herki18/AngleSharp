namespace AngleSharp.Renderer
{
    using Css;
    using Dom;

    public class DocumentRenderer
    {
        private readonly IWindow _window;
        private readonly IRenderDevice _device;
        private readonly IRenderer _renderer;
        private RenderTreeBuilder _builder;
        private IRenderNode? _root;


        public DocumentRenderer(IWindow window, IRenderDevice device, IRenderer renderer)
        {
            _window = window;
            _device = device;
            _renderer = renderer;

            _builder = new RenderTreeBuilder(window, device);
        }

        public void Update()
        {
            _root = _builder.RenderDocument();

            if (_root != null)
            {
                var layoutEngine = new LayoutEngineV2();

                // Get device viewport or fallback to some defaults
                float viewportWidth = 800f;
                float viewportHeight = 600f;

                layoutEngine.LayoutDocument(_root, viewportWidth, viewportHeight);
            }

            _renderer.Render(_root!);
        }

        public IRenderNode GetRoot()
        {
            return _root!;
        }
    }

    public interface IRenderElement
    {
        public T GetElement<T>();
        public void SetLayout(LayoutBox layout);
    }

    public interface IRenderer
    {
        public void Render(IRenderNode node);
    }
}