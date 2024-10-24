namespace AngleSharp.ViewSync
{
    using System;
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
        void AddNode(INode? currentNode, INode childNode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <param name="currentNode"></param>
        /// <param name="childNode"></param>
        void InsertNode(Int32 index, INode currentNode, INode childNode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="node"></param>
        void RemoveNode(INode node);

        /// <summary>
        ///
        /// </summary>
        /// <param name="currentNode"></param>
        void UpdateText(INode currentNode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventSyncedArgs"></param>
        void SyncEvent(Object sender, EventSyncedArgs eventSyncedArgs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventUnregisteredArgs"></param>
        void UnregisterEvent(Object sender, EventUnregisteredArgs eventUnregisteredArgs);
    }
}