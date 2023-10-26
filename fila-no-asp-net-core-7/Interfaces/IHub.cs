namespace fila_no_asp_net_core_7.Interfaces
{
    /// <summary>
    /// Interface defining common grounds for a Hub class, processing a queue and providing a list of processed.
    /// </summary>
    /// <typeparam name="TProcess"></typeparam>
    /// <typeparam name="TProcessed"></typeparam>
    public interface IHub<TProcess, TProcessed>
    {
        /// <summary>
        /// Indicates whether the hub is processing the queue or not
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Queue to Process
        /// </summary>
        Queue<TProcess> Process { get; }

        /// <summary>
        /// List of Already Processed
        /// </summary>
        List<TProcessed> Processed { get; }

        /// <summary>
        /// Process the Process Queue
        /// </summary>
        Task ProcessQueue();

        /// <summary>
        /// Process the <paramref name="request"/>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        bool ProcessRequest(TProcess request);
    }
}
