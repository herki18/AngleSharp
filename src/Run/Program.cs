using System;
using System.Collections.Generic;

namespace Run
{
    using AngleSharp;
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using System.Threading.Tasks;
    using AngleSharp.ViewSync;

    // Observable class representing the subject (DOM or VisualElement)
    public class Observable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        public void Notify(T value)
        {
            foreach (var observer in observers)
            {
                observer.OnNext(value);
            }
        }

        public void Complete()
        {
            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<T>> _observers;
            private IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
    }

    // Observer for DOM changes
    public class DomObserver : IObserver<VisualElement>
    {
        private IElement _domElement;

        public DomObserver(IElement domElement)
        {
            _domElement = domElement;
        }

        // Update DOM when VisualElement changes
        public void OnNext(VisualElement visualElement)
        {
            _domElement.SetAttribute("value", visualElement.text);
            Console.WriteLine($"DOM updated with new value: {_domElement.GetAttribute("value")}");
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    // Observer for VisualElement changes
    public class VisualElementObserver : IObserver<IElement>
    {
        private VisualElement _visualElement;

        public VisualElementObserver(VisualElement visualElement)
        {
            _visualElement = visualElement;
        }

        // Update VisualElement when DOM changes
        public void OnNext(IElement domElement)
        {
            _visualElement.text = domElement.GetAttribute("value")!;
            Console.WriteLine($"VisualElement updated with new text: {_visualElement.text}");
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    // Example class for VisualElement (placeholder for actual Unity VisualElement)
    public class VisualElement
    {
        public string text = "";
        public string style = "";
        public Observable<VisualElement> TextChanged { get; } = new Observable<VisualElement>();

        public void SetText(string newText)
        {
            text = newText;
            TextChanged.Notify(this); // Notify all observers that text changed
        }
    }

    // Core part: UnityViewSynchronizer tied to the Node (DOM)
    public class UnityViewSynchronizer : IViewSynchronizer
    {
        private VisualElement _visualElement;
        private IElement _domElement;
        private DomObserver _domObserver;
        private VisualElementObserver _visualObserver;

        // Initialization with bidirectional observers
        public UnityViewSynchronizer(VisualElement visualElement, IElement domElement)
        {
            _visualElement = visualElement;
            _domElement = domElement;

            // Initialize observers
            _domObserver = new DomObserver(_domElement);
            _visualObserver = new VisualElementObserver(_visualElement);

            // Subscribe observers
            _visualElement.TextChanged.Subscribe(_domObserver); // VisualElement updates DOM
            var domObservable = new Observable<IElement>();
            domObservable.Subscribe(_visualObserver); // DOM updates VisualElement

            // Simulate DOM event subscription
            domElement.SetAttribute("value", "Initial value");
            domObservable.Notify(domElement); // Initial synchronization
        }

        public void SyncDomToVisual()
        {
            // Additional method to trigger sync from DOM to VisualElement when needed
            _visualObserver.OnNext(_domElement);
        }

        public void SyncVisualToDom()
        {
            // Additional method to trigger sync from VisualElement to DOM when needed
            _domObserver.OnNext(_visualElement);
        }
    }

    // Main Program to test the bidirectional observer pattern
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a new browsing context
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);

            // Create a new document
            var document = await context.OpenNewAsync();

            // Create VisualElement and DOM element (Input field)
            var visualElement = new VisualElement();
            var domElement = document.CreateElement(TagNames.Input);

            // Initialize UnityViewSynchronizer for bidirectional sync
            var sync = new UnityViewSynchronizer(visualElement, domElement);

            // Simulate VisualElement update (e.g., user input in UI)
            visualElement.SetText("User input text");
            sync.SyncVisualToDom(); // Sync VisualElement changes to DOM

            // Simulate DOM change (e.g., programmatic change in DOM)
            domElement.SetAttribute("value", "Updated from DOM");
            sync.SyncDomToVisual(); // Sync DOM changes to VisualElement

            // Output the modified HTML
            Console.WriteLine(document.DocumentElement.OuterHtml);
        }
    }
}
