using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranslateBot.Models;

public class TranslationRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string OriginalText { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string TranslatedText { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TranslationType { get; set; } = string.Empty; // "Address" 或 "Product"

    [MaxLength(100)]
    public string? UserSession { get; set; } // 可選的用戶會話識別

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TimeSpan ProcessingTime { get; set; } // 翻譯處理時間

    public bool IsSuccessful { get; set; } = true;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; } // 如果翻譯失敗，記錄錯誤訊息
}
