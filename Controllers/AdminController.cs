using Microsoft.AspNetCore.Mvc;
using TranslateBot.Services;
using TranslateBot.Models;
using System.IO;

namespace TranslateBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly RagService _ragService;
    private readonly CsvReaderService _csvReaderService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(RagService ragService, CsvReaderService csvReaderService, ILogger<AdminController> logger)
    {
        _ragService = ragService;
        _csvReaderService = csvReaderService;
        _logger = logger;
    }

    /// <summary>
    /// 手動初始化向量資料庫
    /// </summary>
    [HttpPost("initialize-vector-db")]
    public async Task<IActionResult> InitializeVectorDatabase()
    {
        try
        {
            var streetNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "街路名稱.csv");
            var countyNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "縣市名稱.csv");
            var villageReferencePath = Path.Combine(Directory.GetCurrentDirectory(), "村里文字巷中英對照.TXT");

            await _ragService.InitializeDataAsync(_csvReaderService, streetNamesPath, countyNamesPath, villageReferencePath);

            return Ok(new { message = "向量資料庫初始化成功，包含街路名稱、縣市名稱及村里文字巷中英對照資料" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化向量資料庫時發生錯誤");
            return StatusCode(500, new { message = "初始化失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 搜尋相似的參考資料 (測試用)
    /// </summary>
    [HttpGet("search-references")]
    public async Task<IActionResult> SearchReferences([FromQuery] string query, [FromQuery] int topK = 5)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "搜尋查詢不能為空" });
            }

            var results = await _ragService.SearchSimilarReferencesAsync(query, topK);

            return Ok(new
            {
                query = query,
                resultsCount = results.Count,
                results = results.Select(r => new
                {
                    content = r.Content,
                    type = r.Type,
                    createdAt = r.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜尋參考資料時發生錯誤");
            return StatusCode(500, new { message = "搜尋失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 取得資料庫統計資訊
    /// </summary>
    [HttpGet("database-stats")]
    public async Task<IActionResult> GetDatabaseStats()
    {
        try
        {
            var context = await _ragService.GetRelevantContextAsync("test");
            // 這裡可以添加更多統計邏輯

            return Ok(new
            {
                message = "資料庫連接正常",
                sampleContext = !string.IsNullOrEmpty(context) ? "已有資料" : "無資料"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得資料庫統計時發生錯誤");
            return StatusCode(500, new { message = "取得統計失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 取得系統統計數據
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            // 從資料庫讀取統計數據
            var (cityCount, streetCount, villageCount, laneCount, totalCount) = await _ragService.GetDatabaseStatsAsync();

            return Ok(new
            {
                cityCount = cityCount,
                streetCount = streetCount,
                villageCount = villageCount,
                laneCount = laneCount,
                totalCount = totalCount,
                translationCount = 0, // 這裡可以從資料庫或日誌中獲取實際的翻譯次數
                lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得系統統計時發生錯誤");
            return StatusCode(500, new { message = "取得統計失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 取得資料概覽
    /// </summary>
    [HttpGet("data")]
    public IActionResult GetDataOverview()
    {
        try
        {
            // 這裡可以返回一些示例數據或最近的記錄
            return Ok(new
            {
                recentTranslations = new object[] { }, // 可以從資料庫中獲取最近的翻譯記錄
                systemHealth = "正常",
                dataStatus = "已初始化"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得資料概覽時發生錯誤");
            return StatusCode(500, new { message = "取得資料概覽失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 測試村里文字巷資料搜索
    /// </summary>
    [HttpGet("test-village-search")]
    public async Task<IActionResult> TestVillageSearch([FromQuery] string query = "一心里", [FromQuery] int topK = 5)
    {
        try
        {
            var results = await _ragService.SearchSimilarReferencesAsync(query, topK);

            var villageResults = results.Where(x => x.Type == "Village").ToList();
            var laneResults = results.Where(x => x.Type == "Lane").ToList();

            return Ok(new
            {
                query = query,
                totalResults = results.Count,
                villageResults = villageResults.Select(r => new
                {
                    content = r.Content,
                    type = r.Type,
                    createdAt = r.CreatedAt
                }),
                laneResults = laneResults.Select(r => new
                {
                    content = r.Content,
                    type = r.Type,
                    createdAt = r.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "測試村里文字巷資料搜索時發生錯誤");
            return StatusCode(500, new { message = "搜索失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 新增地址中英對照資料
    /// </summary>
    [HttpPost("add-address")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 檢查資料是否已存在
            var exists = await _ragService.IsAddressExistsAsync(request.ChineseContent, request.EnglishContent, request.Type);
            if (exists)
            {
                return Conflict(new { message = "相同的地址資料已存在" });
            }

            // 新增資料
            var id = await _ragService.AddAddressReferenceAsync(
                request.ChineseContent,
                request.EnglishContent,
                request.Type,
                request.Note);

            return Ok(new
            {
                message = "地址資料新增成功",
                id = id,
                chineseContent = request.ChineseContent,
                englishContent = request.EnglishContent,
                type = request.Type,
                note = request.Note
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增地址資料時發生錯誤");
            return StatusCode(500, new { message = "新增地址資料失敗", error = ex.Message });
        }
    }

    /// <summary>
    /// 取得支援的地址類型
    /// </summary>
    [HttpGet("address-types")]
    public IActionResult GetAddressTypes()
    {
        try
        {
            var addressTypes = new[]
            {
                new { value = "County", label = "縣市", description = "縣、市、直轄市等行政區劃" },
                new { value = "Street", label = "街路", description = "路、街、大道、巷弄等道路名稱" },
                new { value = "Village", label = "村里", description = "村、里等基層行政區劃" },
                new { value = "Lane", label = "巷弄", description = "巷、弄、衖等細部道路" }
            };

            return Ok(new
            {
                addressTypes = addressTypes,
                message = "支援的地址類型"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得地址類型時發生錯誤");
            return StatusCode(500, new { message = "取得地址類型失敗", error = ex.Message });
        }
    }
}
