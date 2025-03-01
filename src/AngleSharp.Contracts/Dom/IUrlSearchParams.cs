namespace AngleSharp.Dom
{
    using AngleSharp.Attributes;
    using System;

    /// <summary>
    /// Represents a list of query parameters.
    /// </summary>
    [DomName("URLSearchParams")]
    [DomExposed("Window")]
    [DomExposed("Worker")]
    public interface IUrlSearchParams
    {
        /// <summary>
        /// Appends another value for the given search param name.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        /// <param name="value">The value of the param.</param>
        [DomName("append")]
        void Append(String name, String value);

        /// <summary>
        /// Deletes the values of the search param name.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        [DomName("delete")]
        void Delete(String name);

        /// <summary>
        /// Gets the first value of the given search param name, if any.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        /// <returns>The value of the param, if any.</returns>
        [DomName("get")]
        String? Get(String name);

        /// <summary>
        /// Gets all values for the given search param name.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        /// <returns>The list with all stored values.</returns>
        [DomName("getAll")]
        String[] GetAll(String name);

        /// <summary>
        /// Checks if a search param with the given name exists.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        /// <returns>True if such a param exists, otherwise false.</returns>
        [DomName("has")]
        Boolean Has(String name);

        /// <summary>
        /// Sets the given search param.
        /// </summary>
        /// <param name="name">The name of the param.</param>
        /// <param name="value">The value of the param.</param>
        [DomName("set")]
        void Set(String name, String value);

        /// <summary>
        /// Sorts the underlying list.
        /// </summary>
        [DomName("sort")]
        void Sort();
    }
}