using Microsoft.AspNetCore.Mvc;
using TranslateBot.Services;

namespace TranslateBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly TranslationService _translationService;

    public TranslateController(TranslationService translationService)
    {
        _translationService = translationService;
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
            var translatedAddress = await _translationService.TranslateAddressAsync(request.Address);

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
