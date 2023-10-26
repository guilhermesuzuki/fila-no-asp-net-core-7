using fila_no_asp_net_core_7.Interfaces;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace fila_no_asp_net_core_7.Models
{
    [ExcludeFromCodeCoverage]
    public abstract class Hub
    {
        public static readonly object SyncRoot = new();
    }

    [ExcludeFromCodeCoverage]
    public abstract class Hub<TProcess, TProcessed> : Hub,
        IHub<TProcess, TProcessed>
        where TProcess : IRequestId
        where TProcessed : IRequestId
    {
        public bool IsProcessing { get; protected set; }

        public Queue<TProcess> Process { get; protected set; } = new();

        public List<TProcessed> Processed { get; protected set; } = new();

        public virtual Task ProcessQueue()
        {
            if (IsProcessing == false)
            {
                lock (SyncRoot)
                {
                    if (this.IsProcessing == false)
                    {
                        //marks the hub as processing
                        this.IsProcessing = true;

                        return Task.Factory.StartNew(() =>
                        {
                            //while there's stuff in the queue
                            while (this.Process.Count > 0)
                            {
                                //dequeues the request
                                var request = this.Process.Dequeue();

                                try
                                {
                                    //processes the request
                                    this.ProcessRequest(request);
                                }
                                catch (Exception ex)
                                {
                                    //log this error
                                    Log.Error(ex, $"An error happened while processing the request {request.RequestId}");
                                }
                            }

                            //processing is done
                            this.IsProcessing = false;

                        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Open to implementation
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public abstract bool ProcessRequest(TProcess request);
    }
}
