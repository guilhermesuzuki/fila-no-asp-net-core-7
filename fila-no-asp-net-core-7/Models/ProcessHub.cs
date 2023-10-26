using Serilog;
using SixLabors.ImageSharp;
using System.Drawing;
using SixLabors.Fonts;
using System;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;


namespace fila_no_asp_net_core_7.Models
{
    public class ProcessHub : Hub<ProcessRequest, ProcessResponse>
    {
        IWebHostEnvironment _environment;

        /// <summary>
        /// Constructor with DI
        /// </summary>
        /// <param name="environment"></param>
        public ProcessHub(IWebHostEnvironment environment) : base()
        {
            _environment = environment;
        }

        /// <summary>
        /// Process a Request, loading the images and extracting the base64 string, to add in the response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override bool ProcessRequest(ProcessRequest request)
        {
            var response = new ProcessResponse() { RequestId = request.RequestId };

            request.FileNames.AsParallel().ForAll(filename =>
            {
                var path = System.IO.Path.Combine(_environment.WebRootPath, "Images", filename);

                try
                {
                    //needs the image format for the base64string method
                    var format = SixLabors.ImageSharp.Image.DetectFormat(path);

                    //load the image
                    using var image = SixLabors.ImageSharp.Image.Load(path);

                    //adds the file and the base64 to the response
                    response.Images.Add(new() { FileName = filename, Base64 = image.ToBase64String(format), });
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Process Hub failed to process this file {filename}, {RequestId}", filename, request.RequestId);
                }
            });

            //sorts the images by filename to return to sender
            response.Images = response.Images.OrderBy(x => x.FileName).ToList();

            //adds the processed request in the list to go
            lock (SyncRoot)
            {
                Processed.Add(response);
            }

            return true;
        }
    }
}
