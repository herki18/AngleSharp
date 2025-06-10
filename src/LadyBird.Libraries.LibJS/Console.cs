// // File: Console.cs
// using System;
// using System.Collections.Generic;
//
// namespace JS;
//
// using AK;
// using Runtime;
//
// // https://console.spec.whatwg.org
// public class Console
// {
//     // These are not really levels, but that's the term used in the spec.
//     public enum LogLevel
//     {
//         Assert,
//         Count,
//         CountReset,
//         Debug,
//         Dir,
//         DirXML,
//         Error,
//         Group,
//         GroupCollapsed,
//         Info,
//         Log,
//         TimeEnd,
//         TimeLog,
//         Table,
//         Trace,
//         Warn,
//     }
//
//     public struct Group
//     {
//         public string Label;
//     }
//
//     public struct Trace
//     {
//         public string Label;
//         public List<string> Stack;
//     }
//
//     private Realm _realm;
//     private ConsoleClient _client;
//
//     private Dictionary<string, uint> _counters = new();
//     private Dictionary<string, Core.ElapsedTimer> _timerTable = new();
//     private List<Group> _groupStack = new();
//
//     public Console(Realm realm)
//     {
//         _realm = realm;
//     }
//
//     public override void VisitEdges(Visitor visitor)
//     {
//         base.VisitEdges(visitor);
//         visitor.Visit(_realm);
//         visitor.Visit(_client);
//     }
//
//     public void SetClient(ConsoleClient client) => _client = client;
//
//     public Realm Realm => _realm;
//
//     public List<Value> VmArguments()
//     {
//         var vm = _realm.Vm();
//         var arguments = new List<Value>();
//         for (var i = 0; i < vm.ArgumentCount(); ++i)
//         {
//             arguments.Add(vm.Argument(i));
//         }
//         return arguments;
//     }
//
//     public Dictionary<string, uint> Counters() => _counters;
//     public IReadOnlyDictionary<string, uint> CountersReadOnly() => _counters;
//
//     // C++: ThrowCompletionOr<Value> Assert_();
//     public Result<Value> Assert_()
//     {
//         var vm = _realm.Vm();
//
//         // 1. If condition is true, return.
//         var condition = vm.Argument(0).ToBoolean();
//         if (condition)
//             return Result<Value>.Success(Value.JsUndefined());
//
//         // 2. Let message be a string without any formatting specifiers indicating generically an assertion failure (such as "Assertion failed").
//         var message = PrimitiveString.Create(vm, "Assertion failed");
//
//         // NOTE: Assemble `data` from the function arguments.
//         var data = new List<Value>();
//         if (vm.ArgumentCount() > 1)
//         {
//             for (var i = 1; i < vm.ArgumentCount(); ++i)
//             {
//                 data.Add(vm.Argument(i));
//             }
//         }
//
//         // 3. If data is empty, append message to data.
//         if (data.Count == 0)
//         {
//             data.Add(message);
//         }
//         // 4. Otherwise:
//         else
//         {
//             // 1. Let first be data[0].
//             var first = data[0];
//             // 2. If first is not a String, then prepend message to data.
//             if (!first.IsString())
//             {
//                 data.Insert(0, message);
//             }
//             // 3. Otherwise:
//             else
//             {
//                 // 1. Let concat be the concatenation of message, U+003A (:), U+0020 SPACE, and first.
//                 var concat = $"{message.Utf8String()}: {first.ToString(vm)}";
//                 // 2. Set data[0] to concat.
//                 data[0] = PrimitiveString.Create(vm, concat);
//             }
//         }
//
//         // 5. Perform Logger("assert", data).
//         if (_client != null)
//             return _client.Logger(LogLevel.Assert, data);
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: Value Clear();
//     public Value Clear()
//     {
//         // 1. Empty the appropriate group stack.
//         _groupStack.Clear();
//
//         // 2. If possible for the environment, clear the console. (Otherwise, do nothing.)
//         _client?.Clear();
//         return Value.JsUndefined();
//     }
//
//     // C++: ThrowCompletionOr<Value> Debug();
//     public Result<Value> Debug()
//     {
//         if (_client != null)
//         {
//             var data = VmArguments();
//             return _client.Logger(LogLevel.Debug, data);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Error();
//     public Result<Value> Error()
//     {
//         if (_client != null)
//         {
//             var data = VmArguments();
//             return _client.Logger(LogLevel.Error, data);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Info();
//     public Result<Value> Info()
//     {
//         if (_client != null)
//         {
//             var data = VmArguments();
//             return _client.Logger(LogLevel.Info, data);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Log();
//     public Result<Value> Log()
//     {
//         if (_client != null)
//         {
//             var data = VmArguments();
//             return _client.Logger(LogLevel.Log, data);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Table();
//     public Result<Value> Table()
//     {
//         if (_client == null)
//             return Result<Value>.Success(Value.JsUndefined());
//
//         var vm = _realm.Vm();
//
//         if (vm.ArgumentCount() > 0)
//         {
//             var tabularData = vm.Argument(0);
//             var propertiesArg = vm.Argument(1);
//
//             var properties = new Dictionary<PropertyKey, bool>();
//
//             if (propertiesArg.IsArray(vm))
//             {
//                 var propertiesArray = propertiesArg.AsArray().IndexedProperties();
//                 foreach (var prop in propertiesArray)
//                 {
//                     var colName = prop.Value;
//                     properties[PropertyKey.FromValue(vm, colName)] = true;
//                 }
//             }
//
//             var finalRows = new List<Value>();
//             var finalColumns = new List<Value>();
//             var visitedColumns = new Dictionary<PropertyKey, bool>();
//
//             if (tabularData.IsArray(vm))
//             {
//                 var array = tabularData.AsArray();
//                 var indices = array.IndexedProperties();
//
//                 foreach (var prop in indices)
//                 {
//                     var index = new PropertyKey(prop.Index);
//                     var value = array.Get(index);
//
//                     var row = CreateTableRow(_realm, new Value(index.AsNumber()), value, finalColumns, visitedColumns, properties);
//                     finalRows.Add(row);
//                 }
//             }
//             else if (tabularData.IsObject())
//             {
//                 var obj = tabularData.AsObject();
//                 obj.EnumerateObjectProperties((key) =>
//                 {
//                     var index = PropertyKey.FromValue(vm, key);
//                     var value = obj.Get(index);
//
//                     var row = CreateTableRow(_realm, key, value, finalColumns, visitedColumns, properties);
//                     finalRows.Add(row);
//                 });
//             }
//
//             if (finalRows.Count > 0)
//             {
//                 var tableRows = Array.CreateFrom(_realm, finalRows);
//                 var tableCols = Array.CreateFrom(_realm, finalColumns);
//
//                 var finalData = Object.Create(_realm, null);
//                 finalData.Set(vm.Names.Rows, tableRows, Object.ShouldThrowExceptions.No);
//                 finalData.Set(vm.Names.Columns, tableCols, Object.ShouldThrowExceptions.No);
//
//                 var args = new List<Value> { finalData };
//                 return _client.Printer(LogLevel.Table, args);
//             }
//         }
//
//         return _client.Printer(LogLevel.Log, VmArguments());
//     }
//
//     // C++: ThrowCompletionOr<Value> Trace();
//     public Result<Value> Trace()
//     {
//         if (_client == null)
//             return Result<Value>.Success(Value.JsUndefined());
//
//         var vm = _realm.Vm();
//
//         // 1. Let trace be some implementation-defined, potentially-interactive representation of the callstack from where this function was called.
//         var trace = new Trace
//         {
//             Stack = new List<string>()
//         };
//         var executionContextStack = vm.ExecutionContextStack();
//         // NOTE: -2 to skip the console.trace() execution context
//         for (var i = executionContextStack.Count - 2; i >= 0; --i)
//         {
//             var functionName = executionContextStack[i].FunctionName;
//             trace.Stack.Add(string.IsNullOrEmpty(functionName) ? "<anonymous>" : functionName);
//         }
//
//         // 2. Optionally, let formattedData be the result of Formatter(data), and incorporate formattedData as a label for trace.
//         if (vm.ArgumentCount() > 0)
//         {
//             var data = VmArguments();
//             var formattedData = _client.Formatter(data);
//             trace.Label = ValueVectorToString(formattedData.Value);
//         }
//
//         // 3. Perform Printer("trace", « trace »).
//         return _client.Printer(LogLevel.Trace, trace);
//     }
//
//     // C++: ThrowCompletionOr<Value> Warn();
//     public Result<Value> Warn()
//     {
//         if (_client != null)
//         {
//             var data = VmArguments();
//             return _client.Logger(LogLevel.Warn, data);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Dir();
//     public Result<Value> Dir()
//     {
//         var vm = _realm.Vm();
//
//         var obj = vm.Argument(0);
//
//         if (_client != null)
//         {
//             var printerArguments = new List<Value> { obj };
//             return _client.Printer(LogLevel.Dir, printerArguments);
//         }
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Count();
//     public Result<Value> Count()
//     {
//         var vm = _realm.Vm();
//
//         var label = LabelOrFallback(vm, "default");
//
//         var map = _counters;
//
//         if (map.TryGetValue(label, out var count))
//         {
//             map[label] = count + 1;
//         }
//         else
//         {
//             map[label] = 1;
//         }
//
//         var concat = $"{label}: {map[label]}";
//
//         var concatAsVector = new List<Value> { PrimitiveString.Create(vm, concat) };
//         if (_client != null)
//             _client.Logger(LogLevel.Count, concatAsVector);
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> CountReset();
//     public Result<Value> CountReset()
//     {
//         var vm = _realm.Vm();
//
//         var label = LabelOrFallback(vm, "default");
//
//         var map = _counters;
//
//         if (map.ContainsKey(label))
//         {
//             map[label] = 0;
//         }
//         else
//         {
//             var message = $"\"{label}\" doesn't have a count";
//             var messageAsVector = new List<Value> { PrimitiveString.Create(vm, message) };
//             if (_client != null)
//                 _client.Logger(LogLevel.CountReset, messageAsVector);
//         }
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Group();
//     public Result<Value> Group()
//     {
//         var group = new Group();
//
//         var data = VmArguments();
//         string groupLabel;
//         if (data.Count > 0)
//         {
//             var formattedData = _client.Formatter(data);
//             groupLabel = ValueVectorToString(formattedData.Value);
//         }
//         else
//         {
//             groupLabel = "Group";
//         }
//
//         group.Label = groupLabel;
//
//         if (_client != null)
//             _client.Printer(LogLevel.Group, group);
//
//         _groupStack.Add(group);
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> GroupCollapsed();
//     public Result<Value> GroupCollapsed()
//     {
//         var group = new Group();
//
//         var data = VmArguments();
//         string groupLabel;
//         if (data.Count > 0)
//         {
//             var formattedData = _client.Formatter(data);
//             groupLabel = ValueVectorToString(formattedData.Value);
//         }
//         else
//         {
//             groupLabel = "Group";
//         }
//
//         group.Label = groupLabel;
//
//         if (_client != null)
//             _client.Printer(LogLevel.GroupCollapsed, group);
//
//         _groupStack.Add(group);
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> GroupEnd();
//     public Result<Value> GroupEnd()
//     {
//         if (_groupStack.Count == 0)
//             return Result<Value>.Success(Value.JsUndefined());
//
//         _groupStack.RemoveAt(_groupStack.Count - 1);
//         _client?.EndGroup();
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> Time();
//     public Result<Value> Time()
//     {
//         var vm = _realm.Vm();
//
//         var label = LabelOrFallback(vm, "default");
//
//         if (_timerTable.ContainsKey(label))
//         {
//             if (_client != null)
//             {
//                 var timerAlreadyExistsWarningMessageAsVector = new List<Value>
//                 {
//                     PrimitiveString.Create(vm, $"Timer '{label}' already exists.")
//                 };
//                 _client.Printer(LogLevel.Warn, timerAlreadyExistsWarningMessageAsVector);
//             }
//             return Result<Value>.Success(Value.JsUndefined());
//         }
//
//         _timerTable[label] = Core.ElapsedTimer.StartNew();
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> TimeLog();
//     public Result<Value> TimeLog()
//     {
//         var vm = _realm.Vm();
//
//         var label = LabelOrFallback(vm, "default");
//
//         if (!_timerTable.TryGetValue(label, out var startTime))
//         {
//             if (_client != null)
//             {
//                 var timerDoesNotExistWarningMessageAsVector = new List<Value>
//                 {
//                     PrimitiveString.Create(vm, $"Timer '{label}' does not exist.")
//                 };
//                 _client.Printer(LogLevel.Warn, timerDoesNotExistWarningMessageAsVector);
//             }
//             return Result<Value>.Success(Value.JsUndefined());
//         }
//
//         var duration = AK.HumanReadableTime(startTime.ElapsedTime());
//
//         var concat = $"{label}: {duration}";
//
//         var data = new List<Value> { PrimitiveString.Create(vm, concat) };
//         for (var i = 1; i < vm.ArgumentCount(); ++i)
//             data.Add(vm.Argument(i));
//
//         if (_client != null)
//             _client.Printer(LogLevel.TimeLog, data);
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<Value> TimeEnd();
//     public Result<Value> TimeEnd()
//     {
//         var vm = _realm.Vm();
//
//         var label = LabelOrFallback(vm, "default");
//
//         if (!_timerTable.TryGetValue(label, out var startTime))
//         {
//             if (_client != null)
//             {
//                 var timerDoesNotExistWarningMessageAsVector = new List<Value>
//                 {
//                     PrimitiveString.Create(vm, $"Timer '{label}' does not exist.")
//                 };
//                 _client.Printer(LogLevel.Warn, timerDoesNotExistWarningMessageAsVector);
//             }
//             return Result<Value>.Success(Value.JsUndefined());
//         }
//
//         _timerTable.Remove(label);
//
//         var duration = AK.HumanReadableTime(startTime.ElapsedTime());
//
//         var concat = $"{label}: {duration}";
//
//         if (_client != null)
//         {
//             var concatAsVector = new List<Value> { PrimitiveString.Create(vm, concat) };
//             _client.Printer(LogLevel.TimeEnd, concatAsVector);
//         }
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     public void OutputDebugMessage(LogLevel logLevel, string output)
//     {
//         switch (logLevel)
//         {
//             case LogLevel.Debug:
//                 Console.WriteLine($"\u001b[32;1m(js debug)\u001b[0m {output}");
//                 break;
//             case LogLevel.Error:
//                 Console.WriteLine($"\u001b[32;1m(js error)\u001b[0m {output}");
//                 break;
//             case LogLevel.Info:
//                 Console.WriteLine($"\u001b[32;1m(js info)\u001b[0m {output}");
//                 break;
//             case LogLevel.Log:
//                 Console.WriteLine($"\u001b[32;1m(js log)\u001b[0m {output}");
//                 break;
//             case LogLevel.Warn:
//                 Console.WriteLine($"\u001b[32;1m(js warn)\u001b[0m {output}");
//                 break;
//             default:
//                 Console.WriteLine($"\u001b[32;1m(js)\u001b[0m {output}");
//                 break;
//         }
//     }
//
//     public void ReportException(Error exception, bool inPromise)
//     {
//         _client?.ReportException(exception, inPromise);
//     }
//
//     public string ValueVectorToString(List<Value> values)
//     {
//         var vm = _realm.Vm();
//         var builder = new System.Text.StringBuilder();
//
//         foreach (var item in values)
//         {
//             if (builder.Length > 0)
//                 builder.Append(' ');
//
//             builder.Append(item.ToString(vm));
//         }
//
//         return builder.ToString();
//     }
//
//     // Helper for label fallback
//     private string LabelOrFallback(VM vm, string fallback)
//     {
//         return vm.ArgumentCount() > 0 && !vm.Argument(0).IsUndefined()
//             ? vm.Argument(0).ToString(vm)
//             : fallback;
//     }
//
//     // C++: static ThrowCompletionOr<GC::Ref<Object>> create_table_row(...)
//     private static Value CreateTableRow(
//         Realm realm,
//         Value rowIndex,
//         Value tabularDataItem,
//         List<Value> finalColumns,
//         Dictionary<PropertyKey, bool> visitedColumns,
//         Dictionary<PropertyKey, bool> properties)
//     {
//         var vm = realm.Vm();
//
//         void AddColumn(PropertyKey columnName)
//         {
//             if (!visitedColumns.ContainsKey(columnName))
//             {
//                 visitedColumns[columnName] = true;
//
//                 if (columnName.IsString())
//                 {
//                     finalColumns.Add(PrimitiveString.Create(vm, columnName.AsString()));
//                 }
//                 else if (columnName.IsSymbol())
//                 {
//                     finalColumns.Add(columnName.AsSymbol());
//                 }
//                 else if (columnName.IsNumber())
//                 {
//                     finalColumns.Add(new Value(columnName.AsNumber()));
//                 }
//             }
//         }
//
//         var row = Object.Create(realm, null);
//
//         var key = new PropertyKey("(index)", PropertyKey.StringMayBeNumber.No);
//         row.Set(key, rowIndex, Object.ShouldThrowExceptions.No);
//
//         AddColumn(key);
//
//         if (tabularDataItem.IsArray(vm))
//         {
//             var array = tabularDataItem.AsArray();
//             var indices = array.IndexedProperties();
//
//             foreach (var prop in indices)
//             {
//                 var propKey = new PropertyKey(prop.Index);
//                 var value = array.Get(propKey);
//
//                 if (properties.Count > 0 && !properties.ContainsKey(propKey))
//                     continue;
//
//                 row.Set(propKey, value, Object.ShouldThrowExceptions.No);
//                 AddColumn(propKey);
//             }
//         }
//         else if (tabularDataItem.IsObject())
//         {
//             var obj = tabularDataItem.AsObject();
//             obj.EnumerateObjectProperties((keyV) =>
//             {
//                 var propKey = PropertyKey.FromValue(vm, keyV);
//
//                 if (properties.Count > 0 && !properties.ContainsKey(propKey))
//                     return;
//
//                 row.Set(propKey, obj.Get(propKey), Object.ShouldThrowExceptions.No);
//                 AddColumn(propKey);
//             });
//         }
//         else
//         {
//             row.Set(vm.Names.Value, tabularDataItem, Object.ShouldThrowExceptions.No);
//             AddColumn(vm.Names.Value);
//         }
//
//         return row;
//     }
// }
//
// public abstract class ConsoleClient
// {
//     public delegate object PrinterArguments();
//
//     protected Console _console;
//
//     public ConsoleClient(Console console)
//     {
//         _console = console;
//     }
//
//     public override void VisitEdges(Visitor visitor)
//     {
//         base.VisitEdges(visitor);
//         visitor.Visit(_console);
//     }
//
//     // C++: ThrowCompletionOr<Value> Logger(Console::LogLevel logLevel, GC::RootVector<Value> const& args)
//     public virtual Result<Value> Logger(Console.LogLevel logLevel, List<Value> args)
//     {
//         var vm = _console.Realm.Vm();
//
//         if (args.Count == 0)
//             return Result<Value>.Success(Value.JsUndefined());
//
//         var first = args[0];
//         var restSize = args.Count - 1;
//
//         if (restSize == 0)
//         {
//             var firstAsVector = new List<Value> { first };
//             return Printer(logLevel, firstAsVector);
//         }
//         else
//         {
//             var formatted = Formatter(args);
//             Printer(logLevel, formatted.Value);
//         }
//
//         return Result<Value>.Success(Value.JsUndefined());
//     }
//
//     // C++: ThrowCompletionOr<GC::RootVector<Value>> Formatter(GC::RootVector<Value> const& args)
//     public virtual Result<List<Value>> Formatter(List<Value> args)
//     {
//         var realm = _console.Realm;
//         var vm = realm.Vm();
//
//         if (args.Count == 1)
//             return Result<List<Value>>.Success(args);
//
//         var target = args.Count > 0 ? args[0].ToString(vm) : string.Empty;
//         var current = args.Count > 1 ? args[1] : Value.JsUndefined();
//
//         string FindSpecifier(string targetStr)
//         {
//             var startIndex = 0;
//             while (startIndex < targetStr.Length)
//             {
//                 var index = targetStr.IndexOf('%', startIndex);
//                 if (index == -1 || index + 1 >= targetStr.Length)
//                     return null;
//
//                 switch (targetStr[index + 1])
//                 {
//                     case 'c':
//                     case 'd':
//                     case 'f':
//                     case 'i':
//                     case 'o':
//                     case 'O':
//                     case 's':
//                         return targetStr.Substring(index, 2);
//                 }
//                 startIndex = index + 1;
//             }
//             return null;
//         }
//         var maybeSpecifier = FindSpecifier(target);
//
//         if (maybeSpecifier == null)
//         {
//             return Result<List<Value>>.Success(args);
//         }
//         else
//         {
//             var specifier = maybeSpecifier;
//             Value converted = null;
//
//             if (specifier == "%s")
//             {
//                 converted = Call(vm, realm.Intrinsics.StringConstructor(), Value.JsUndefined(), current);
//             }
//             else if (specifier == "%d" || specifier == "%i")
//             {
//                 if (current.IsSymbol())
//                 {
//                     converted = Value.JsNaN();
//                 }
//                 else
//                 {
//                     converted = Call(vm, realm.Intrinsics.ParseIntFunction(), Value.JsUndefined(), current, new Value(10));
//                 }
//             }
//             else if (specifier == "%f")
//             {
//                 if (current.IsSymbol())
//                 {
//                     converted = Value.JsNaN();
//                 }
//                 else
//                 {
//                     converted = Call(vm, realm.Intrinsics.ParseFloatFunction(), Value.JsUndefined(), current);
//                 }
//             }
//             else if (specifier == "%o")
//             {
//                 converted = current;
//             }
//             else if (specifier == "%O")
//             {
//                 converted = current;
//             }
//             else if (specifier == "%c")
//             {
//                 AddCssStyleToCurrentMessage(current.ToString(vm));
//                 converted = PrimitiveString.Create(vm, string.Empty);
//             }
//
//             if (converted != null)
//                 target = target.Replace(specifier, converted.ToString(vm), StringComparison.Ordinal);
//         }
//
//         var result = new List<Value> { PrimitiveString.Create(vm, target) };
//         for (var i = 2; i < args.Count; ++i)
//             result.Add(args[i]);
//
//         return Formatter(result);
//     }
//
//     public abstract Result<Value> Printer(Console.LogLevel logLevel, object printerArguments);
//
//     public virtual void AddCssStyleToCurrentMessage(string style) { }
//     public virtual void ReportException(Error exception, bool inPromise) { }
//     public abstract void Clear();
//     public abstract void EndGroup();
//
//     public string GenericallyFormatValues(List<Value> values)
//     {
//         var stream = new AllocatingMemoryStream();
//         var vm = _console.Realm.Vm();
//         var ctx = new PrintContext(vm, stream, true);
//         var first = true;
//         foreach (var value in values)
//         {
//             if (!first)
//                 stream.WriteUntilDepleted(" ");
//             JS.Print(value, ctx);
//             first = false;
//         }
//         return stream.ToString();
//     }
//
//     // C++: static Value Call(...)
//     private static Value Call(VM vm, object function, Value thisValue, params Value[] args)
//     {
//         // Stub for JS::call
//         return Value.JsUndefined();
//     }
// }