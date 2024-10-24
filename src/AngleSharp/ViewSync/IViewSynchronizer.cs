namespace AngleSharp.ViewSync
{
    using Dom;

    /// <summary>
    /// Represents a view synchronizer
    /// </summary>
    public interface IViewSynchronizer
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetView<T>() where T : class;

        /// <summary>
        ///
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="childNode"></param>
        void UpdateParent(INode? currentNode, INode childNode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="currentNode"></param>
        void UpdateText(INode currentNode);
    }
}