using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CarRetalWebsite.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, string category);
        void DeleteImage(string imageUrl);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _imageFolderRoot;

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
            // image folder is at the repo root level: up two levels from project ContentRootPath
            var repoRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", ".."));
            _imageFolderRoot = Path.Combine(repoRoot, "image");
        }

        public async Task<string> SaveImageAsync(IFormFile file, string category)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            // Ensure category folder (e.g. "Car" or "User") exists
            var categoryFolder = Path.Combine(_imageFolderRoot, category);
            if (!Directory.Exists(categoryFolder))
            {
                Directory.CreateDirectory(categoryFolder);
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{category.ToLower()}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(categoryFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return path format used for serving statically and storing in DB
            return $"/image/{category}/{uniqueFileName}";
        }

        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            // Expect path format: /image/{category}/{fileName}
            var pathParts = imageUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length < 3 || !pathParts[0].Equals("image", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var category = pathParts[1];
            var fileName = pathParts[2];
            var filePath = Path.Combine(_imageFolderRoot, category, fileName);

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore delete errors to avoid breaking flows if file is locked or missing
                }
            }
        }
    }
}
