using Serilog;

namespace fila_no_asp_net_core_7.Models
{
    public class SaveHub : Hub<SaveRequest, SaveResponse>
    {
        static readonly string imageBmp = "data:image/bmp;base64,";
        static readonly string imageGif = "data:image/gif;base64,";
        static readonly string imageJpeg = "data:image/jpeg;base64,";
        static readonly string imageJpg = "data:image/jpg;base64,";
        static readonly string imagePng = "data:image/png;base64,";
        static readonly string imageTiff = "data:image/tiff;base64,";

        /// <summary>
        /// The environment is used to retrieve the wwwroot path
        /// </summary>
        readonly IWebHostEnvironment _environment;

        public SaveHub(IWebHostEnvironment environment)
        {
            this._environment = environment;
        }

        /// <summary>
        /// Process the Save Request, saving the images with the respective file names.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override bool ProcessRequest(SaveRequest request)
        {
            try
            {
                if (request.Images.Count > 0)
                {
                    //images with row numbers attached to each row
                    var images = request.Images.GroupBy(x => x.FileName).SelectMany(g => g.Select((img, i) => new { Image = img, RowNnumber = i + 1 }));

                    //images to save (only the last ones in the request will be processed, since we are overwriting them anyway)
                    var imagesSave = images.Where(x => x.RowNnumber == images.OrderBy(x => x.RowNnumber).Last(y => y.Image.FileName == x.Image.FileName).RowNnumber);

                    //processes the images in parallel
                    imagesSave.AsParallel().ForAll(image =>
                    {
                        var base64 = image.Image.Base64;
                        var cmp = StringComparison.CurrentCultureIgnoreCase;

                        //could have used regular expression for this, but performance
                        if (base64.StartsWith(imagePng, cmp)) base64 = base64.Substring(22);
                        else if (base64.StartsWith(imageJpg, cmp)) base64 = base64.Substring(22);
                        else if (base64.StartsWith(imageBmp, cmp)) base64 = base64.Substring(22);
                        else if (base64.StartsWith(imageGif, cmp)) base64 = base64.Substring(22);
                        else if (base64.StartsWith(imageJpeg, cmp)) base64 = base64.Substring(23);
                        else if (base64.StartsWith(imageTiff, cmp)) base64 = base64.Substring(23);

                        var bytes = Convert.FromBase64String(base64);
                        using var img = SixLabors.ImageSharp.Image.Load(bytes);

                        lock (SyncRoot)
                        {
                            img.SaveAsJpeg(Path.Combine(_environment.WebRootPath, "Images", image.Image.FileName));
                        }
                    });

                    lock (SyncRoot)
                    {
                        this.Processed.Add(new SaveResponse { RequestId = request.RequestId, Status = "Saved" });
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Save Hub failed to process this Request {RequestId}", request.RequestId);
                this.Processed.Add(new SaveResponse { RequestId = request.RequestId, Status = "Failed" });
                return false;
            }
        }
    }
}
