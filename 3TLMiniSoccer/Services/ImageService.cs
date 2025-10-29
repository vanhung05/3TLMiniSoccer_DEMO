using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.Drawing.Imaging;

namespace _3TLMiniSoccer.Services
{
    public class ImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> TaiLenHinhAnhAsync(IFormFile file, string category, bool generateThumbnail = true)
        {
            try
            {
                // Validate file
                if (!KiemTraHinhAnhHopLeAsync(file))
                {
                    throw new ArgumentException("File không hợp lệ");
                }

                // Create directory if not exists
                var categoryPath = Path.Combine("wwwroot", "images", category);
                var fullPath = Path.Combine(_environment.ContentRootPath, categoryPath);
                Directory.CreateDirectory(fullPath);

                // Generate unique filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{category}_{timestamp}_{Guid.NewGuid().ToString("N")[..8]}.jpg";
                var filePath = Path.Combine(fullPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Optimize image
                await ToiUuHoaHinhAnhAsync(filePath);

                // Generate thumbnail if requested
                if (generateThumbnail)
                {
                    await TaoThumbnailAsync(filePath, 400, 300);
                }

                _logger.LogInformation($"Image uploaded successfully: {fileName}");
                return $"/images/{category}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image: {ex.Message}");
                throw;
            }
        }

        public async Task<string> TaiLenHinhAnhSanAsync(IFormFile file, int fieldId, bool generateThumbnail = true)
        {
            try
            {
                // Validate file
                if (!KiemTraHinhAnhHopLeAsync(file))
                {
                    throw new ArgumentException("File không hợp lệ");
                }

                // Create directory if not exists
                var categoryPath = Path.Combine("wwwroot", "images", "san");
                var fullPath = Path.Combine(_environment.ContentRootPath, categoryPath);
                Directory.CreateDirectory(fullPath);

                // Generate filename theo quy luật san{id}.jpg
                var fileName = $"san{fieldId}.jpg";
                var filePath = Path.Combine(fullPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Optimize image
                await ToiUuHoaHinhAnhAsync(filePath);

                // Generate thumbnail if requested
                if (generateThumbnail)
                {
                    await TaoThumbnailAsync(filePath, 400, 300);
                }

                _logger.LogInformation($"Field image uploaded successfully: {fileName}");
                return fileName; // Trả về chỉ tên file để lưu vào database
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading field image: {ex.Message}");
                throw;
            }
        }

        public async Task XoaHinhAnhAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return;

                // Remove leading slash
                var cleanPath = imagePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.ContentRootPath, cleanPath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Image deleted: {imagePath}");
                }

                // Delete thumbnails
                var thumbnailsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "images", "thumbnails");
                var fileName = Path.GetFileNameWithoutExtension(fullPath);
                var thumbnails = Directory.GetFiles(thumbnailsPath, $"{fileName}_*");

                foreach (var thumbnail in thumbnails)
                {
                    File.Delete(thumbnail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image: {ex.Message}");
                throw;
            }
        }

        public async Task<string> TaoThumbnailAsync(string imagePath, int width, int height)
        {
            try
            {
                var cleanPath = imagePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.ContentRootPath, cleanPath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("Image not found");

                // Create thumbnails directory
                var thumbnailsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "images", "thumbnails");
                Directory.CreateDirectory(thumbnailsPath);

                // Generate thumbnail filename
                var fileName = Path.GetFileNameWithoutExtension(fullPath);
                var thumbnailName = $"{fileName}_thumb_{width}x{height}.jpg";
                var thumbnailPath = Path.Combine(thumbnailsPath, thumbnailName);

                // Create thumbnail
                using (var originalImage = Image.FromFile(fullPath))
                using (var thumbnail = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(thumbnail))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // Calculate aspect ratio
                    var aspectRatio = (double)originalImage.Width / originalImage.Height;
                    var targetAspectRatio = (double)width / height;

                    int sourceX, sourceY, sourceWidth, sourceHeight;

                    if (aspectRatio > targetAspectRatio)
                    {
                        // Image is wider
                        sourceHeight = originalImage.Height;
                        sourceWidth = (int)(originalImage.Height * targetAspectRatio);
                        sourceX = (originalImage.Width - sourceWidth) / 2;
                        sourceY = 0;
                    }
                    else
                    {
                        // Image is taller
                        sourceWidth = originalImage.Width;
                        sourceHeight = (int)(originalImage.Width / targetAspectRatio);
                        sourceX = 0;
                        sourceY = (originalImage.Height - sourceHeight) / 2;
                    }

                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                _logger.LogInformation($"Thumbnail created: {thumbnailName}");
                return $"/images/thumbnails/{thumbnailName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating thumbnail: {ex.Message}");
                throw;
            }
        }

        public string LayDuongDanThumbnailAsync(string imagePath, string size = "md")
        {
            if (string.IsNullOrEmpty(imagePath))
                return "/images/placeholder.jpg";

            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var sizeMap = new Dictionary<string, (int, int)>
            {
                ["sm"] = (100, 100),
                ["md"] = (200, 200),
                ["lg"] = (400, 400),
                ["wide"] = (400, 300),
                ["tall"] = (300, 400)
            };

            if (sizeMap.TryGetValue(size, out var dimensions))
            {
                return $"/images/thumbnails/{fileName}_thumb_{dimensions.Item1}x{dimensions.Item2}.jpg";
            }

            return $"/images/thumbnails/{fileName}_thumb_200x200.jpg";
        }

        public bool KiemTraHinhAnhHopLeAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
                return false;

            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return false;

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }

        public async Task ToiUuHoaHinhAnhAsync(string imagePath, int quality = 80)
        {
            try
            {
                var cleanPath = imagePath.TrimStart('/');
                var fullPath = Path.Combine(_environment.ContentRootPath, cleanPath);

                if (!File.Exists(fullPath))
                    return;

                // Create temporary file to avoid GDI+ issues
                var tempPath = fullPath + ".tmp";
                
                using (var originalImage = Image.FromFile(fullPath))
                {
                    // Create optimized image
                    var encoder = ImageCodecInfo.GetImageDecoders()
                        .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                    // Save to temporary file first
                    originalImage.Save(tempPath, encoder, encoderParams);
                }

                // Replace original file with optimized version
                if (File.Exists(tempPath))
                {
                    File.Replace(tempPath, fullPath, null);
                }

                _logger.LogInformation($"Image optimized: {imagePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error optimizing image: {ex.Message}");
            }
        }
    }
}
