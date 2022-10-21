using ImageUploader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Drawing;
using System.Security.Cryptography.Xml;

namespace ImageUploader.Controllers
{
    public class ImagesController : Controller
    {
        private readonly AzureStorageConfig _storageConfig = null;
        // set maximum image dimensions
        private const int MAX_WIDTH = 1024;
        private const int MAX_HEIGHT = 1024;


        public ImagesController(IOptions<AzureStorageConfig> config)
        {
            _storageConfig = config.Value;
        }


        /// <summary>
        /// Checks if the given file is jpg or png
        /// </summary>
        /// <param name="file">Image provided by user</param>
        /// <returns>Boolean stating if file provided is the correct data-type</returns>
        public static bool IsImage(IFormFile file)
        {
            bool isFileImage = false;
            if (file.ContentType.Contains("image"))
            {
                string[] formats = new string[] { ".jpg", ".png" };
                isFileImage = formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
            }

            return isFileImage;
        }


        /// <summary>
        /// Checks if the provided image is within the dev-specified dimensions
        /// </summary>
        /// <param name="file">Image provided by user</param>
        /// <returns>Boolean stating if image is acceptably sized</returns>
        public static bool IsImageWithinDimensions(IFormFile file)
        {
            bool isImageSizeGood = true;

            using (var image = Image.FromStream(file.OpenReadStream()))
            {
                if (image.Width > MAX_WIDTH || image.Height > MAX_HEIGHT)
                {
                    isImageSizeGood = false;
                }
            }

            return isImageSizeGood;
        }


        /// <summary>
        /// Uploads the image to Azure Blob Storage
        /// </summary>
        /// <param name="stream">File stream we're reading image from</param>
        /// <param name="fileName">File's name</param>
        /// <returns>Boolean indicating successful upload</returns>
        public async Task<bool> UploadImage(Stream stream, string fileName)
        {
            // Create a blob client
            Uri blobUri = new Uri("https://" + _storageConfig.AccountName + ".blob.core.windows.net/" + _storageConfig.AccountName + "/" + fileName);
            
            // Read configuration settings 
            StorageSharedKeyCredential storageCredentials = new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);
            
            BlobClient blobClient = new BlobClient(blobUri, storageCredentials);

            // Upload image to blob storage
            await blobClient.UploadAsync(stream);

            return await Task.FromResult(true);
        }


        [HttpPost("[upload]")]
        public async Task<IActionResult> Upload(ICollection<IFormFile> files)
        {
            bool isUploaded = false;
            string response = "";
            try
            {
                // If no file are given
                if (files.Count == 0)
                    response += "No files were uploaded.\n";
                
                // If authentication hasn't been provided
                if(_storageConfig.AccountKey == String.Empty || _storageConfig.AccountName == String.Empty)
                {
                    response += "No authentication provided\n";
                }

                // If no image container has been provided
                if(_storageConfig.ImageContainer == String.Empty)
                {
                    response += "No image container provided\n";
                }

                // For each file provided
                foreach(var formFile in files)
                {   
                    // if file is an image
                    if (IsImage(formFile))
                    {
                        // 
                        if(formFile.Length > 0)
                        {
                            using(Stream stream = formFile.OpenReadStream())
                            {
                                isUploaded = await UploadImage(stream, formFile.FileName);
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                response += ex.Message;
            }
            return response != "" ? BadRequest(response) : Ok("File uploaded successfully");
        }
    }
}
