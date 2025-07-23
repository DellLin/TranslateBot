using System.ComponentModel.DataAnnotations;

namespace TranslateBot.Models;

public class AddAddressRequest
{
    /// <summary>
    /// 中文地址內容
    /// </summary>
    [Required(ErrorMessage = "中文地址不能為空")]
    [MaxLength(500, ErrorMessage = "中文地址不能超過500個字元")]
    public string ChineseContent { get; set; } = string.Empty;

    /// <summary>
    /// 英文地址內容
    /// </summary>
    [Required(ErrorMessage = "英文地址不能為空")]
    [MaxLength(500, ErrorMessage = "英文地址不能超過500個字元")]
    public string EnglishContent { get; set; } = string.Empty;

    /// <summary>
    /// 地址類型 (Street, County, Village, Lane)
    /// </summary>
    [Required(ErrorMessage = "地址類型不能為空")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 備註
    /// </summary>
    [MaxLength(200, ErrorMessage = "備註不能超過200個字元")]
    public string? Note { get; set; }
}
