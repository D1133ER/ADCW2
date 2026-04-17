using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.IO;
using System.Threading.Tasks;
using WeblogApplication.Interfaces;

namespace WeblogApplication.Implementation
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ImageService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var compressedImage = await TinyImageAsync(imageFile);

            var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            var uniqueFileName = Guid.NewGuid().ToString() + "_image.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            await System.IO.File.WriteAllBytesAsync(filePath, compressedImage);
            return "/uploads/" + uniqueFileName;
        }

        private async Task<byte[]> TinyImageAsync(IFormFile imageFile)
        {
            using (var inputStream = imageFile.OpenReadStream())
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var image = Image.Load(inputStream))
                    {
                        var quality = 75;
                        image.Save(outputStream, new JpegEncoder { Quality = quality });

                        while (outputStream.Length > 3 * 1024 * 1024)
                        {
                            outputStream.SetLength(0);
                            quality -= 5;
                            image.Save(outputStream, new JpegEncoder { Quality = quality });
                            outputStream.Seek(0, SeekOrigin.Begin);
                        }
                        return outputStream.ToArray();
                    }
                }
            }
        }
    }
}
