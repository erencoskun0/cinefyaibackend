namespace CinefyAiServer.Services;

/// <summary>
/// Dosya işlemleri için servis interface'i
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Film afişi yükler
    /// </summary>
    /// <param name="file">Yüklenecek dosya</param>
    /// <returns>Dosya URL'i</returns>
    Task<string> UploadMovieImageAsync(IFormFile file);

    /// <summary>
    /// Dosyayı siler
    /// </summary>
    /// <param name="fileName">Silinecek dosya adı</param>
    /// <returns>Silme işlemi başarılı mı?</returns>
    Task<bool> DeleteFileAsync(string fileName);

    /// <summary>
    /// Dosya tipinin geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="file">Kontrol edilecek dosya</param>
    /// <returns>Dosya geçerli mi?</returns>
    bool IsValidImageFile(IFormFile file);
}