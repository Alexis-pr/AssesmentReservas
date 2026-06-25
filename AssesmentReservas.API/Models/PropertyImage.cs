using System.ComponentModel.DataAnnotations;

namespace AssesmentReservas.API.Models;

/// <summary>Foto de un inmueble. El binario vive en MinIO; aquí guardamos solo la llave.</summary>
public class PropertyImage
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    /// <summary>Object key dentro del bucket de MinIO.</summary>
    [MaxLength(300)]
    public string ObjectKey { get; set; } = default!;

    public bool IsCover { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
