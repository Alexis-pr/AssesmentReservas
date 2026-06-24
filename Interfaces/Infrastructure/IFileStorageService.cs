namespace AssesmentReservas.API.Interfaces.Infrastructure;

/// <summary>Abstracción de almacenamiento de objetos (MinIO / S3).</summary>
public interface IFileStorageService
{
    /// <summary>Sube un objeto y devuelve su object key. Crea el bucket si no existe.</summary>
    Task<string> UploadAsync(string bucket, Stream content, string fileName, string contentType,
        bool publicRead = false, CancellationToken ct = default);

    Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken ct = default);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default);

    /// <summary>URL accesible desde el navegador (usa el endpoint público).</summary>
    string BuildPublicUrl(string bucket, string objectKey);
}
