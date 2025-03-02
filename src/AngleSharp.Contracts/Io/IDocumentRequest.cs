namespace AngleSharp.Io;

using System;
using System.Collections.Generic;
using System.IO;
using Dom;

public interface IDocumentRequest
{
    INode? Source { get; set; }

    /// <summary>
    ///     Gets or sets the referrer of the request, if any. The name is
    ///     intentionally spelled wrong, to emphasize the relationship with the
    ///     HTTP header.
    /// </summary>
    String? Referer { get; set; }

    HttpMethod Method { get; set; }

    Stream Body { get; set; }

    String? MimeType { get; set; }

    Dictionary<String, String> Headers { get; }
}