namespace AssesmentReservas.API.Settings;

/// <summary>Configuración de MinIO (compatible S3) para almacenamiento de objetos.</summary>
public class MinioSettings
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "http://minio:9000";

    /// <summary>Endpoint accesible desde el navegador para construir URLs públicas (ej: http://localhost:9000).</summary>
    public string PublicEndpoint { get; set; } = "http://localhost:9000";

    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;

    /// <summary>Bucket público para imágenes de inmuebles.</summary>
    public string PublicBucket { get; set; } = "properties";

    /// <summary>Bucket privado para documentos KYC (cifrados).</summary>
    public string PrivateBucket { get; set; } = "kyc-documents";

    public bool UseSsl { get; set; }
}
