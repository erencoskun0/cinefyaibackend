namespace CinefyAiServer.Services;

/// <summary>
/// Dosya işlemleri için servis implementasyonu
/// </summary>
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

    public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Film afişi yükler
    /// </summary>
    public async Task<string> UploadMovieImageAsync(IFormFile file)
    {
        try
        {
            if (!IsValidImageFile(file))
            {
                throw new ArgumentException("Geçersiz dosya tipi");
            }

            // Uploads klasörünü oluştur
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "movies");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Benzersiz dosya adı oluştur
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Dosyayı kaydet
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // URL'i döndür
            var fileUrl = $"/uploads/movies/{fileName}";

            _logger.LogInformation("Movie image uploaded successfully: {FileName}", fileName);

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading movie image: {FileName}", file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Dosyayı siler
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileName.TrimStart('/'));

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Dosya tipinin geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > _maxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            return false;

        // MIME type kontrolü
        var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }
}