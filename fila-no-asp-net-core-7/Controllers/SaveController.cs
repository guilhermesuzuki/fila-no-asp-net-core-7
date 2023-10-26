using fila_no_asp_net_core_7.Interfaces;
using fila_no_asp_net_core_7.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace fila_no_asp_net_core_7.Controllers
{
    [Route("[Controller]")]
    public class SaveController : Controller
    {
        /// <summary>
        /// SaveRequest Hub
        /// </summary>
        IHub<SaveRequest, SaveResponse> _theHub;

        public SaveController(IHub<SaveRequest, SaveResponse> hub): base()
        {
            this._theHub = hub;
        }

        [HttpPost]
        [EnableRateLimiting("fixed")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SaveResponse> Index([FromBody] SaveRequest request)
        {
            try
            {
                lock (Hub.SyncRoot) _theHub.Process.Enqueue(request);

                //processes the queue
                _theHub.ProcessQueue();

                //waits for the request to be processed
                var done = this.WaitUntilTheRequestIsProcessed(request);

                //or times out in 300 seconds
                var task = Task.WhenAny(done, Task.Delay(TimeSpan.FromSeconds(60 * 5)));

                return Ok(new SaveResponse
                {
                    RequestId = request.RequestId,
                    Status = task.Result.Id == done.Id ? "Saved" : "Failed",
                });
            }
            finally
            {
                lock (Hub.SyncRoot)
                {
                    var toBeRemoved = this._theHub.Processed.FirstOrDefault(x => x.RequestId == request.RequestId);
                    if (toBeRemoved != null) this._theHub.Processed.Remove(toBeRemoved);
                }
            }
        }

        /// <summary>
        /// Wait for the <paramref name="request"/> to be added in the Processed list.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task WaitUntilTheRequestIsProcessed(SaveRequest request)
        {
            var task = Task.Factory.StartNew(() => {
                //iterates until the request is processed
                while (_theHub.Processed.ToList().Where(x => x != null).Any(x => x.RequestId == request.RequestId) == false) { }
            });

            return task;
        }
    }
}
