using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

public class CustomConsole
{
    private readonly Dictionary<string, Stopwatch> timers = new Dictionary<string, Stopwatch>();

    public void Time(string label)
    {
        if (!timers.ContainsKey(label))
        {
            var stopwatch = new Stopwatch();
            timers[label] = stopwatch;
            stopwatch.Start();
        }
        else
        {
            Console.WriteLine($"Timer '{label}' is already running.");
        }
    }

    public void TimeEnd(string label)
    {
        if (timers.ContainsKey(label))
        {
            var stopwatch = timers[label];
            stopwatch.Stop();

            // Calculate elapsed time in seconds for higher precision
            double elapsedSeconds = stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

            // Output the elapsed time with precision (up to microseconds)
            Console.WriteLine($"{label}: {elapsedSeconds * 1000:0.000000} ms");

            timers.Remove(label);
        }
        else
        {
            Console.WriteLine($"No timer found for label '{label}'.");
        }
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}

public class BenchmarkObject
{
    public int Value { get; set; }

    public BenchmarkObject(int initialValue)
    {
        Value = initialValue;
    }

    public static int Add(int a, int b)
    {
        return a + b;
    }

    public static void BatchAdd(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var result = Add(i, i);
        }
    }

    public static void BatchProcessParallel(int count)
    {
        Parallel.For(0, count, i =>
        {
            var result = Add(i, i);
        });
    }

    public void IncrementValue()
    {
        Value++;
    }

    public static void BatchDelegate(Func<int, int, int> addFunc, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var result = addFunc(i, i);
        }
    }

    public static void BatchAction(Action<int, int> action, int count)
    {
        for (int i = 0; i < count; i++)
        {
            action(i, i);
        }
    }

    public static void LoopWithCallback(Action<int> jsCallback, int count)
    {
        for (int i = 0; i < count; i++)
        {
            jsCallback(i);
        }
    }
}

public class OptimizedJavaScriptBenchmark
{
    public static void Main(string[] args)
    {
        new BenchmarkExecutor().RunBenchmark();
    }
}

public class BenchmarkExecutor
{
    public void RunBenchmark()
    {
        using (var engine = new V8ScriptEngine(V8ScriptEngineFlags.DisableGlobalMembers))
        {
            engine.Execute("1 + 1;");
            var customConsole = new CustomConsole();
            engine.Script.console = new
            {
                time = new Action<string>(customConsole.Time),
                timeEnd = new Action<string>(customConsole.TimeEnd),
                log = new Action<string>(customConsole.Log)
            };

            engine.AddHostType("BenchmarkObject", typeof(BenchmarkObject));
            var initializedObject = new BenchmarkObject(0);
            engine.AddHostObject("initializedObject", initializedObject);

            Func<int, int, int> addDelegate = BenchmarkObject.Add;
            Action<int, int> addAction = (a, b) => BenchmarkObject.Add(a, b);

            engine.AddHostObject("addDelegate", addDelegate);
            engine.AddHostObject("addAction", addAction);

            string script = @"
                var jsCallback = function(i) {
                    if (i % 1000000 === 0) console.log('Callback from C# with i:', i);
                };

                var tickCount = 0;
                function tick() {
                    tickCount++;
                    console.log('Tick called: ' + tickCount);
                    if (tickCount >= 10) {
                        return false;
                    }
                    return true;
                }
            ";
            engine.Execute(script);
            dynamic jsCallback = engine.Script.jsCallback;

            script = @"
                function benchmarkWithCSharp() {
                    console.time('With C# Interaction');
                    BenchmarkObject.BatchAdd(10000000);
                    console.timeEnd('With C# Interaction');
                }

                function benchmarkWithParallelCSharp() {
                    console.time('Parallel C# Interaction');
                    BenchmarkObject.BatchProcessParallel(10000000);
                    console.timeEnd('Parallel C# Interaction');
                }

                function benchmarkWithoutCSharp() {
                    console.time('Without C# Interaction');
                    for (let i = 0; i < 10000000; i++) {
                        let result = i + i;
                    }
                    console.timeEnd('Without C# Interaction');
                }

                function benchmarkInitializedObject() {
                    console.time('Initialized Object Interaction');
                    for (let i = 0; i < 10000000; i++) {
                        initializedObject.IncrementValue();
                    }
                    console.timeEnd('Initialized Object Interaction');
                }

                function benchmarkWithDelegate() {
                    console.time('With Delegate');
                    BenchmarkObject.BatchDelegate(addDelegate, 10000000);
                    console.timeEnd('With Delegate');
                }

                function benchmarkWithAction() {
                    console.time('With Action');
                    BenchmarkObject.BatchAction(addAction, 10000000);
                    console.timeEnd('With Action');
                }

                benchmarkWithCSharp();
                benchmarkWithParallelCSharp();
                benchmarkWithoutCSharp();
                benchmarkInitializedObject();
                benchmarkWithDelegate();
                benchmarkWithAction();
            ";
            var compiledScript = engine.Compile(script);
            engine.Execute(compiledScript);

            customConsole.Log("Running C# loop calling JS callback...");
            Stopwatch sw = Stopwatch.StartNew();
            BenchmarkObject.LoopWithCallback(new Action<int>(i => jsCallback(i)), 10000000);
            sw.Stop();
            customConsole.Log($"C# Loop with JS Callback: {sw.ElapsedMilliseconds} ms");

            // customConsole.Log("Running Tick loop...");
            // StartTickLoop(engine.Script.tick, 500);
        }
    }

    private static void StartTickLoop(Func<bool> tickFunc, int intervalMs)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        while (true)
        {
            if (sw.ElapsedMilliseconds >= intervalMs)
            {
                bool continueTicking = tickFunc();

                if (!continueTicking)
                {
                    Console.WriteLine("Stopping Tick loop.");
                    break;
                }

                sw.Restart();
            }

            Thread.Sleep(1);
        }
    }
}
