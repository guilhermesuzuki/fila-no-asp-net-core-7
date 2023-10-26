using fila_no_asp_net_core_7.Interfaces;
using fila_no_asp_net_core_7.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace fila_no_asp_net_core_7.Controllers
{
    [Route("[Controller]")]
    public class ProcessController : Controller
    {
        /// <summary>
        /// SaveRequest Hub
        /// </summary>
        IHub<ProcessRequest, ProcessResponse> _theHub;

        /// <summary>
        /// Constructor with DI
        /// </summary>
        /// <param name="hub">the Process Request Hub</param>
        public ProcessController(IHub<ProcessRequest, ProcessResponse> hub) : base()
        {
            this._theHub = hub;
        }

        [HttpPost]
        [EnableRateLimiting("fixed")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<ProcessResponse> Index([FromBody] ProcessRequest request)
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

                //task is done
                if (task.Result.Id == done.Id)
                {
                    return Ok(_theHub.Processed.ToList().FirstOrDefault(x => x.RequestId == request.RequestId));
                }

                //I looked for a timeout status code error, but could not find one
                return this.StatusCode(StatusCodes.Status500InternalServerError);
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
        Task WaitUntilTheRequestIsProcessed(ProcessRequest request)
        {
            var task = Task.Factory.StartNew(() => {
                //iterates until the request is processed
                while (_theHub.Processed.ToList().Where(x => x != null).Any(x => x.RequestId == request.RequestId) == false) { }
            });

            return task;
        }
    }
}
