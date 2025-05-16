namespace AngleSharp.Io;

using System;
using System.Collections.Generic;
using System.IO;
using Dom;
using Html;

public interface IDocumentRequest
{
    /// <summary>
    /// Gets or sets the source of the request, if any.
    /// </summary>
    INode? Source { get; set; }

    /// <summary>
    /// Gets the target of the request.
    /// </summary>
    IUrl Target { get; }

    /// <summary>
    /// Gets or sets the referrer of the request, if any. The name is
    /// intentionally spelled wrong, to emphasize the relationship with the
    /// HTTP header.
    /// </summary>
    String? Referer { get; set; }

    /// <summary>
    /// Gets or sets the method to use.
    /// </summary>
    HttpMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the stream of the request's body.
    /// </summary>
    Stream Body { get; set; }

    /// <summary>
    /// Gets or sets the mime-type to use, if any.
    /// </summary>
    String? MimeType { get; set; }

    /// <summary>
    /// Gets a list of headers (key-values) that should be used.
    /// </summary>
    Dictionary<String, String> Headers { get; }
}