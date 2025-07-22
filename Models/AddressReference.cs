using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace TranslateBot.Models;

public class AddressReference
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty; // "Street" 或 "County"

    [Required]
    public Vector Embedding { get; set; } = new Vector(Array.Empty<float>());

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
