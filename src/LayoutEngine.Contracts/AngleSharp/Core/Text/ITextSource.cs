#nullable disable
namespace AngleSharp.Text;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

/// <summary>
/// Text source with read/write capabilities
/// </summary>
public interface ITextSource : IReadOnlyTextSource
{
    /// <summary>
    /// Inserts content at current mark and moves it
    /// </summary>
    void InsertText(String content);
}

/// <summary>
/// Read-only text source
/// </summary>
public interface IReadOnlyTextSource : IDisposable
{
    String Text { get; }
    Int32 Length { get; }
    Encoding CurrentEncoding { get; set; }
    Int32 Index { get; set; }
    Char this[Int32 index] { get; }

    /// <summary>
    /// Reads next character
    /// </summary>
    Char ReadCharacter();

    /// <summary>
    /// Reads specified number of characters
    /// </summary>
    String ReadCharacters(Int32 characters);

    /// <summary>
    /// Reads characters as string or memory reference
    /// </summary>
    StringOrMemory ReadMemory(Int32 characters);

    /// <summary>
    /// Prefetches specified bytes into buffer
    /// </summary>
    Task PrefetchAsync(Int32 length, CancellationToken cancellationToken);

    /// <summary>
    /// Prefetches entire stream
    /// </summary>
    Task PrefetchAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to get content length
    /// </summary>
    Boolean TryGetContentLength(out Int32 length);
}