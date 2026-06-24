using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using AssesmentReservas.API.Interfaces.Infrastructure;
using AssesmentReservas.API.Settings;
using Microsoft.Extensions.Options;

namespace AssesmentReservas.API.Services.Infrastructure;

/// <summary>Implementación de almacenamiento sobre MinIO usando el SDK de AWS S3.</summary>
public class MinioStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    // Evita re-verificar el bucket en cada subida dentro del ciclo de vida del proceso.
    private static readonly ConcurrentDictionary<string, bool> _ensuredBuckets = new();

    public MinioStorageService(IAmazonS3 s3, IOptions<MinioSettings> settings, ILogger<MinioStorageService> logger)
    {
        _s3 = s3;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string bucket, Stream content, string fileName, string contentType,
        bool publicRead = false, CancellationToken ct = default)
    {
        await EnsureBucketAsync(bucket, publicRead, ct);

        var ext = Path.GetExtension(fileName);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = true
        }, ct);

        _logger.LogInformation("Objeto subido a {Bucket}/{Key}", bucket, key);
        return key;
    }

    public async Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(bucket, objectKey, ct);
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default)
        => _s3.DeleteObjectAsync(bucket, objectKey, ct);

    public string BuildPublicUrl(string bucket, string objectKey)
        => $"{_settings.PublicEndpoint.TrimEnd('/')}/{bucket}/{objectKey}";

    private async Task EnsureBucketAsync(string bucket, bool publicRead, CancellationToken ct)
    {
        if (_ensuredBuckets.ContainsKey(bucket))
            return;

        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket);
        if (!exists)
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, ct);

        if (publicRead)
        {
            var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [{
                "Effect": "Allow",
                "Principal": "*",
                "Action": ["s3:GetObject"],
                "Resource": ["arn:aws:s3:::{{bucket}}/*"]
              }]
            }
            """;
            await _s3.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = bucket,
                Policy = policy
            }, ct);
        }

        _ensuredBuckets[bucket] = true;
    }
}
