namespace AngleSharp.Io;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using Dom;

    public interface IResponse : IDisposable
    {
        HttpStatusCode StatusCode { get; }
        IUrl Address { get; }
        IDictionary<String, String> Headers { get; }
        Stream Content { get; }
    }