namespace AngleSharp.Media
{
    using AngleSharp.Dom;

    /// <summary>
    /// Specifies general resource information.
    /// </summary>
    public interface IResourceInfo
    {
        /// <summary>
        /// Gets the source of the resource.
        /// </summary>
        IUrl Source { get; set; }
    }
}
