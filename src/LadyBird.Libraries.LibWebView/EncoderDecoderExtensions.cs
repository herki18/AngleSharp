// C# translation of C++ template specializations for encode/decode

namespace LadyBird.Libraries.WebView;

using System.Collections.Generic;
using System.Text;
using AK;

public static class EncoderDecoderExtensions
{
    // From C++: template<> ErrorOr<void> encode(Encoder&, WebView::ConsoleLog const&);
    public static Result<Unit> Encode(this Encoder encoder, global::LadyBird.Libraries.WebView.ConsoleLog log)
    {
        // C++: TRY(encoder.encode(log.level));
        var result = encoder.Encode(log.Level);
        if (result.IsFailure) return Result.Failure(result.Error);

        // C++: TRY(encoder.encode(log.arguments));
        result = encoder.Encode(log.Arguments);
        if (result.IsFailure) return Result.Failure(result.Error);

        return Result.Success();
    }

    // From C++: template<> ErrorOr<WebView::ConsoleLog> decode(Decoder&);
    public static Result<global::LadyBird.Libraries.WebView.ConsoleLog> DecodeConsoleLog(this Decoder decoder)
    {
        var levelResult = decoder.Decode<JS.Console.LogLevel>();
        if (levelResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleLog>(levelResult.Error);

        var argumentsResult = decoder.Decode<List<AK.JsonValue>>();
        if (argumentsResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleLog>(argumentsResult.Error);

        var log = new global::LadyBird.Libraries.WebView.ConsoleLog
        {
            Level = levelResult.Value,
            Arguments = argumentsResult.Value
        };
        return Result.Success(log);
    }

    // From C++: template<> ErrorOr<void> encode(Encoder&, WebView::StackFrame const&);
    public static Result<Unit> Encode(this Encoder encoder, global::LadyBird.Libraries.WebView.StackFrame frame)
    {
        var result = encoder.Encode(frame.Function);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(frame.File);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(frame.Line);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(frame.Column);
        if (result.IsFailure) return Result.Failure(result.Error);

        return Result.Success();
    }

    // From C++: template<> ErrorOr<WebView::StackFrame> decode(Decoder&);
    public static Result<global::LadyBird.Libraries.WebView.StackFrame> DecodeStackFrame(this Decoder decoder)
    {
        var functionResult = decoder.Decode<string?>();
        if (functionResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.StackFrame>(functionResult.Error);

        var fileResult = decoder.Decode<string?>();
        if (fileResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.StackFrame>(fileResult.Error);

        var lineResult = decoder.Decode<ulong?>();
        if (lineResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.StackFrame>(lineResult.Error);

        var columnResult = decoder.Decode<ulong?>();
        if (columnResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.StackFrame>(columnResult.Error);

        var frame = new global::LadyBird.Libraries.WebView.StackFrame
        {
            Function = functionResult.Value,
            File = fileResult.Value,
            Line = lineResult.Value,
            Column = columnResult.Value
        };
        return Result.Success(frame);
    }

    // From C++: template<> ErrorOr<void> encode(Encoder&, WebView::ConsoleError const&);
    public static Result<Unit> Encode(this Encoder encoder, global::LadyBird.Libraries.WebView.ConsoleError error)
    {
        var result = encoder.Encode(error.Name);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(error.Message);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(error.Trace);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(error.InsidePromise);
        if (result.IsFailure) return Result.Failure(result.Error);

        return Result.Success();
    }

    // From C++: template<> ErrorOr<WebView::ConsoleError> decode(Decoder&);
    public static Result<global::LadyBird.Libraries.WebView.ConsoleError> DecodeConsoleError(this Decoder decoder)
    {
        var nameResult = decoder.Decode<string>();
        if (nameResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleError>(nameResult.Error);

        var messageResult = decoder.Decode<string>();
        if (messageResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleError>(messageResult.Error);

        var traceResult = decoder.Decode<List<global::LadyBird.Libraries.WebView.StackFrame>>();
        if (traceResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleError>(traceResult.Error);

        var insidePromiseResult = decoder.Decode<bool>();
        if (insidePromiseResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleError>(insidePromiseResult.Error);

        var error = new global::LadyBird.Libraries.WebView.ConsoleError
        {
            Name = nameResult.Value,
            Message = messageResult.Value,
            Trace = traceResult.Value,
            InsidePromise = insidePromiseResult.Value
        };
        return Result.Success(error);
    }

    // From C++: template<> ErrorOr<void> encode(Encoder&, WebView::ConsoleOutput const&);
    public static Result<Unit> Encode(this Encoder encoder, global::LadyBird.Libraries.WebView.ConsoleOutput output)
    {
        var result = encoder.Encode(output.Timestamp);
        if (result.IsFailure) return Result.Failure(result.Error);

        result = encoder.Encode(output.Output);
        if (result.IsFailure) return Result.Failure(result.Error);

        return Result.Success();
    }

    // From C++: template<> ErrorOr<WebView::ConsoleOutput> decode(Decoder&);
    public static Result<global::LadyBird.Libraries.WebView.ConsoleOutput> DecodeConsoleOutput(this Decoder decoder)
    {
        var timestampResult = decoder.Decode<AK.UnixDateTime>();
        if (timestampResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleOutput>(timestampResult.Error);

        var outputResult = decoder.Decode<AK.Variant<global::LadyBird.Libraries.WebView.ConsoleLog, global::LadyBird.Libraries.WebView.ConsoleError>>();
        if (outputResult.IsFailure) return Result.Failure<global::LadyBird.Libraries.WebView.ConsoleOutput>(outputResult.Error);

        var output = new global::LadyBird.Libraries.WebView.ConsoleOutput
        {
            Timestamp = timestampResult.Value,
            Output = outputResult.Value
        };
        return Result.Success(output);
    }
}