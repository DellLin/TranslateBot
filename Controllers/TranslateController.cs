using Microsoft.AspNetCore.Mvc;
using TranslateBot.Services;

namespace TranslateBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly TranslationService _translationService;
    private readonly TranslationRecordService _recordService;

    public TranslateController(TranslationService translationService, TranslationRecordService recordService)
    {
        _translationService = translationService;
        _recordService = recordService;
    }

    [HttpPost("address")]
    public async Task<IActionResult> TranslateAddress([FromBody] TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new { error = "Address is required" });
        }

        try
        {
            var userSession = HttpContext.Request.Headers["X-User-Session"].FirstOrDefault() ??
                             HttpContext.Connection.RemoteIpAddress?.ToString();

            var translatedAddress = await _translationService.TranslateAddressAsync(request.Address, userSession);

            return Ok(new TranslateResponse
            {
                OriginalAddress = request.Address,
                TranslatedAddress = translatedAddress
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Translation failed", details = ex.Message });
        }
    }

    [HttpPost("product")]
    public async Task<IActionResult> TranslateProduct([FromBody] TranslateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest(new { error = "Product name is required" });
        }

        try
        {
            var userSession = HttpContext.Request.Headers["X-User-Session"].FirstOrDefault() ??
                             HttpContext.Connection.RemoteIpAddress?.ToString();

            var translatedProductName = await _translationService.TranslateProductNameAsync(request.ProductName, userSession);

            return Ok(new TranslateProductResponse
            {
                OriginalProductName = request.ProductName,
                TranslatedProductName = translatedProductName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Product translation failed", details = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetTranslationHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? userSession = null)
    {
        try
        {
            // 如果沒有指定 userSession，使用當前請求的會話
            if (string.IsNullOrEmpty(userSession))
            {
                userSession = HttpContext.Request.Headers["X-User-Session"].FirstOrDefault() ??
                             HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            var history = await _recordService.GetTranslationHistoryAsync(page, pageSize, type, userSession);
            var totalCount = await _recordService.GetTotalRecordsCountAsync(type, userSession);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new
            {
                data = history,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve translation history", details = ex.Message });
        }
    }

    [HttpGet("history/{id}")]
    public async Task<IActionResult> GetTranslationRecord(int id)
    {
        try
        {
            var record = await _recordService.GetTranslationRecordAsync(id);

            if (record == null)
            {
                return NotFound(new { error = "Translation record not found" });
            }

            return Ok(record);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve translation record", details = ex.Message });
        }
    }

    [HttpGet("history/search")]
    public async Task<IActionResult> SearchTranslationHistory(
        [FromQuery] string searchText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return BadRequest(new { error = "Search text is required" });
        }

        try
        {
            var results = await _recordService.SearchTranslationsAsync(searchText, page, pageSize);

            return Ok(new
            {
                data = results,
                searchText = searchText,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Search failed", details = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetTranslationStatistics()
    {
        try
        {
            var statistics = await _recordService.GetTranslationStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve statistics", details = ex.Message });
        }
    }
}

public class TranslateRequest
{
    public string Address { get; set; } = string.Empty;
}

public class TranslateResponse
{
    public string OriginalAddress { get; set; } = string.Empty;
    public string TranslatedAddress { get; set; } = string.Empty;
}

public class TranslateProductRequest
{
    public string ProductName { get; set; } = string.Empty;
}

public class TranslateProductResponse
{
    public string OriginalProductName { get; set; } = string.Empty;
    public string TranslatedProductName { get; set; } = string.Empty;
}
