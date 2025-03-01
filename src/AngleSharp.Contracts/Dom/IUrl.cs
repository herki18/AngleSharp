namespace AngleSharp.Dom
{
    using AngleSharp.Attributes;
    using System;

    /// <summary>
    /// Represents a URL interface according to RFC3986.
    /// Provides access to components of a URL and methods for URL manipulation.
    /// </summary>
    [DomName("URL")]
    [DomExposed("Window")]
    [DomExposed("Worker")]
    public interface IUrl : IEquatable<IUrl>
    {
        /// <summary>
        /// Gets the origin of the stored url.
        /// </summary>
        [DomName("origin")]
        String? Origin { get; }

        /// <summary>
        /// Gets if the URL parsing resulted in an error.
        /// </summary>
        Boolean IsInvalid { get; }

        /// <summary>
        /// Gets if the stored url is relative.
        /// </summary>
        Boolean IsRelative { get; }

        /// <summary>
        /// Gets if the stored url is absolute.
        /// </summary>
        Boolean IsAbsolute { get; }

        /// <summary>
        /// Gets or sets the username for authorization.
        /// </summary>
        [DomName("username")]
        String? UserName { get; set; }

        /// <summary>
        /// Gets or sets the password for authorization.
        /// </summary>
        [DomName("password")]
        String? Password { get; set; }

        /// <summary>
        /// Gets the additional stored data of the URL. This is data that could
        /// not be assigned.
        /// </summary>
        String Data { get; }

        /// <summary>
        /// Gets or sets the fragment, e.g., "first-section".
        /// </summary>
        String? Fragment { get; set; }

        /// <summary>
        /// Gets or sets the hash, e.g., "#first-section".
        /// </summary>
        [DomName("hash")]
        String Hash { get; set; }

        /// <summary>
        /// Gets or sets the host, e.g. "localhost:8800" or "www.w3.org".
        /// </summary>
        [DomName("host")]
        String Host { get; set; }

        /// <summary>
        /// Gets or sets the host name, e.g. "localhost" or "www.w3.org".
        /// </summary>
        [DomName("hostname")]
        String HostName { get; set; }

        /// <summary>
        /// Gets or sets the hyper reference, i.e. the full URL.
        /// </summary>
        [DomName("href")]
        String Href { get; set; }

        /// <summary>
        /// Gets or sets the path, e.g. "mypath".
        /// </summary>
        String Path { get; set; }

        /// <summary>
        /// Gets or sets the pathname, e.g. "/mypath".
        /// </summary>
        [DomName("pathname")]
        String PathName { get; set; }

        /// <summary>
        /// Gets or sets the port, e.g. "8800".
        /// </summary>
        [DomName("port")]
        String Port { get; set; }

        /// <summary>
        /// Gets or sets the scheme, e.g. "http".
        /// </summary>
        String Scheme { get; set; }

        /// <summary>
        /// Gets or sets the protocol, e.g. "http:".
        /// </summary>
        [DomName("protocol")]
        String Protocol { get; set; }

        /// <summary>
        /// Gets or sets the query part, e.g., "foo=bar".
        /// </summary>
        String? Query { get; set; }

        /// <summary>
        /// Gets or sets the search part, e.g., "?foo=bar".
        /// </summary>
        [DomName("search")]
        String Search { get; set; }

        /// <summary>
        /// Obtains an advanced view on the provided query parameter.
        /// </summary>
        [DomName("searchParams")]
        IUrlSearchParams SearchParams { get; }

        /// <summary>
        /// Serializes the URL string to a JSON compatible string representation.
        /// </summary>
        /// <returns>The currently stored url.</returns>
        [DomName("toJSON")]
        String ToJson();
    }
}