namespace LayoutEngine.Core.UnityMock;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Render;

/// <summary>
/// MonoBehaviour that manages the LayoutEngine and renders HTML content to UI Toolkit
/// </summary>
public class HtmlRenderer : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    [SerializeField, TextArea(5, 20)] private string initialHtml =
        "<div style=\"background-color: white; width: 100%; height: 100%;\"><div style=\"color: blue; font-size: 20px; margin: 20px;\">Hello, World!</div></div>";

    private IServiceProvider _serviceProvider;
    private IEngine _engine;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("UIDocument is required for HtmlRenderer");
                return;
            }
        }

        InitializeServices();
    }

    private void Start()
    {
        LoadHtml(initialHtml);
    }

    private void Update()
    {
        // Update the engine on every frame
        _engine?.Update(Time.deltaTime);
    }

    private void InitializeServices()
    {
        var services = new ServiceCollection();

        // Configure the layout engine
        var config = new LayoutEngineConfiguration
        {
            TargetFramesPerSecond = 60 // This is just a hint, actual updates are controlled by Unity
        };

        // Add core services
        services.AddLayoutEngine(config);

        // Add Unity renderer
        var rootElement = new VisualElement();
        rootElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        rootElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

        uiDocument.rootVisualElement.Add(rootElement);

        // Create and register the renderer
        var renderer = new UnityRenderer(rootElement);
        services.AddSingleton<IRenderer>(renderer);

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();

        // Get engine
        _engine = _serviceProvider.GetRequiredService<IEngine>();

        // Connect renderer to render system
        var renderSystem = _serviceProvider.GetRequiredService<IRenderSystem>();
        renderSystem.AttachRenderer(renderer);
    }

    /// <summary>
    /// Loads HTML content into the renderer
    /// </summary>
    public async Task LoadHtml(string html)
    {
        if (_engine == null)
        {
            Debug.LogError("Engine not initialized");
            return;
        }

        try
        {
            await _engine.OpenAsync(html);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading HTML: {ex}");
        }
    }

    private void OnDestroy()
    {
        // Dispose engine if it's disposable
        if (_engine is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Dispose service provider if it's disposable
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}