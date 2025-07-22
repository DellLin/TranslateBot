using Microsoft.EntityFrameworkCore;
using TranslateBot.Data;
using TranslateBot.Models;
using Pgvector;

namespace TranslateBot.Services;

public class RagService
{
    private readonly TranslateBotDbContext _dbContext;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<RagService> _logger;

    public RagService(TranslateBotDbContext dbContext, EmbeddingService embeddingService, ILogger<RagService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// 將參考資料存入向量資料庫
    /// </summary>
    public async Task StoreReferenceDataAsync(string content, string type)
    {
        try
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(content);

            var addressRef = new AddressReference
            {
                Content = content,
                Type = type,
                Embedding = new Vector(embedding.ToArray())
            };

            _dbContext.AddressReferences.Add(addressRef);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("成功存入參考資料: {Type} - {Content}", type, content.Length > 50 ? content[..50] + "..." : content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存入參考資料時發生錯誤: {Type} - {Content}", type, content);
            throw;
        }
    }

    /// <summary>
    /// 批量存入參考資料
    /// </summary>
    public async Task StoreBatchReferenceDataAsync(IList<(string content, string type)> items, int batchSize = 10)
    {
        try
        {
            _logger.LogInformation("開始批量存入 {TotalCount} 筆參考資料，批次大小: {BatchSize}", items.Count, batchSize);

            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                var batchNumber = (i / batchSize) + 1;
                var totalBatches = (int)Math.Ceiling((double)items.Count / batchSize);

                await ProcessBatchWithRetryAsync(batch, batchNumber, totalBatches);

                // 批次間的短暫延遲，避免過於頻繁的 API 調用
                if (i + batchSize < items.Count)
                {
                    await Task.Delay(1000);
                }
            }

            _logger.LogInformation("成功完成所有批次處理，共處理 {Count} 筆參考資料", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存入參考資料時發生錯誤");
            throw;
        }
    }

    private async Task ProcessBatchWithRetryAsync(IList<(string content, string type)> batch, int batchNumber, int totalBatches)
    {
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount <= maxRetries)
        {
            try
            {
                _logger.LogInformation("處理批次 {BatchNumber}/{TotalBatches}，包含 {Count} 筆資料",
                    batchNumber, totalBatches, batch.Count);

                var contents = batch.Select(x => x.content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(contents);

                var addressRefs = new List<AddressReference>();
                for (int i = 0; i < batch.Count; i++)
                {
                    addressRefs.Add(new AddressReference
                    {
                        Content = batch[i].content,
                        Type = batch[i].type,
                        Embedding = new Vector(embeddings[i].ToArray())
                    });
                }

                _dbContext.AddressReferences.AddRange(addressRefs);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("成功處理批次 {BatchNumber}/{TotalBatches}", batchNumber, totalBatches);
                return; // 成功處理，跳出重試循環
            }
            catch (Exception ex) when (Is429Error(ex) && retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning("批次 {BatchNumber}/{TotalBatches} 遇到 429 錯誤，第 {RetryCount} 次重試，等待 30 秒...",
                    batchNumber, totalBatches, retryCount);

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理批次 {BatchNumber}/{TotalBatches} 時發生錯誤", batchNumber, totalBatches);
                throw;
            }
        }

        throw new InvalidOperationException($"批次 {batchNumber}/{totalBatches} 在 {maxRetries} 次重試後仍然失敗");
    }

    private static bool Is429Error(Exception ex)
    {
        // 檢查是否為 429 錯誤（請求過於頻繁）
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("429") ||
               message.Contains("too many requests") ||
               message.Contains("rate limit") ||
               message.Contains("quota exceeded") ||
               (ex.InnerException != null && Is429Error(ex.InnerException));
    }

    /// <summary>
    /// 搜尋相似的參考資料
    /// </summary>
    public async Task<List<AddressReference>> SearchSimilarReferencesAsync(string query, int topK = 5, double similarityThreshold = 0.7)
    {
        try
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            // 使用 PostgreSQL 向量相似性搜索
            var sql = @"
                SELECT id, content, type, embedding, created_at,
                       1 - (embedding <=> @queryEmbedding) AS similarity
                FROM address_references
                WHERE 1 - (embedding <=> @queryEmbedding) > @threshold
                ORDER BY embedding <=> @queryEmbedding
                LIMIT @topK";

            var results = await _dbContext.AddressReferences
                .FromSqlRaw(sql,
                    new Npgsql.NpgsqlParameter("@queryEmbedding", new Vector(queryEmbedding.ToArray())),
                    new Npgsql.NpgsqlParameter("@threshold", similarityThreshold),
                    new Npgsql.NpgsqlParameter("@topK", topK))
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜尋相似參考資料時發生錯誤: {Query}", query);
            return new List<AddressReference>();
        }
    }

    /// <summary>
    /// 取得格式化的參考內容用於 prompt
    /// </summary>
    public async Task<string> GetRelevantContextAsync(string address)
    {
        var similarReferences = await SearchSimilarReferencesAsync(address, topK: 15, similarityThreshold: 0.6);

        if (!similarReferences.Any())
        {
            return string.Empty;
        }

        var streetRefs = similarReferences.Where(x => x.Type == "Street").Take(3);
        var countyRefs = similarReferences.Where(x => x.Type == "County").Take(3);
        var villageRefs = similarReferences.Where(x => x.Type == "Village").Take(4);
        var laneRefs = similarReferences.Where(x => x.Type == "Lane").Take(5);

        var context = "";

        if (streetRefs.Any())
        {
            context += "**相關街路名稱：**\n";
            foreach (var reference in streetRefs)
            {
                context += $"{reference.Content}\n";
            }
            context += "\n";
        }

        if (countyRefs.Any())
        {
            context += "**相關縣市名稱：**\n";
            foreach (var reference in countyRefs)
            {
                context += $"{reference.Content}\n";
            }
            context += "\n";
        }

        if (villageRefs.Any())
        {
            context += "**相關村里名稱：**\n";
            foreach (var reference in villageRefs)
            {
                context += $"{reference.Content}\n";
            }
            context += "\n";
        }

        if (laneRefs.Any())
        {
            context += "**相關巷弄名稱：**\n";
            foreach (var reference in laneRefs)
            {
                context += $"{reference.Content}\n";
            }
        }

        return context;
    }

    /// <summary>
    /// 初始化資料庫並載入 CSV 資料和村里文字巷中英對照資料
    /// </summary>
    public async Task InitializeDataAsync(CsvReaderService csvReaderService, string streetNamesPath, string countyNamesPath, string? villageReferencePath = null)
    {
        try
        {
            var items = new List<(string content, string type)>();

            // 讀取街路名稱
            if (File.Exists(streetNamesPath))
            {
                var streetLines = File.ReadAllLines(streetNamesPath, System.Text.Encoding.UTF8);
                foreach (var line in streetLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        items.Add((line.Trim(), "Street"));
                    }
                }
            }

            // 讀取縣市名稱
            if (File.Exists(countyNamesPath))
            {
                var countyLines = File.ReadAllLines(countyNamesPath, System.Text.Encoding.UTF8);
                foreach (var line in countyLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        items.Add((line.Trim(), "County"));
                    }
                }
            }

            // 讀取村里文字巷中英對照資料
            if (!string.IsNullOrEmpty(villageReferencePath) && File.Exists(villageReferencePath))
            {
                var villageItems = await LoadVillageReferenceDataAsync(villageReferencePath);
                items.AddRange(villageItems);
            }

            if (items.Any())
            {
                // 過濾掉已經存在的資料
                var filteredItems = await FilterExistingDataAsync(items);

                if (filteredItems.Any())
                {
                    await StoreBatchReferenceDataAsync(filteredItems);
                    _logger.LogInformation("成功初始化 {Count} 筆新的參考資料到向量資料庫", filteredItems.Count);
                }
                else
                {
                    _logger.LogInformation("所有參考資料都已存在於資料庫中，跳過初始化");
                }

                var skippedCount = items.Count - filteredItems.Count;
                if (skippedCount > 0)
                {
                    _logger.LogInformation("跳過了 {SkippedCount} 筆已存在的參考資料", skippedCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化向量資料庫時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 過濾掉已經存在於資料庫中的資料
    /// </summary>
    private async Task<List<(string content, string type)>> FilterExistingDataAsync(IList<(string content, string type)> items)
    {
        try
        {
            var filteredItems = new List<(string content, string type)>();

            // 批量檢查以提高效率
            const int checkBatchSize = 100;
            for (int i = 0; i < items.Count; i += checkBatchSize)
            {
                var batch = items.Skip(i).Take(checkBatchSize).ToList();
                var contents = batch.Select(x => x.content).ToList();

                // 查詢這批資料中哪些已存在
                var existingContents = await _dbContext.AddressReferences
                    .Where(x => contents.Contains(x.Content))
                    .Select(x => x.Content)
                    .ToListAsync();

                // 加入不存在的資料
                foreach (var item in batch)
                {
                    if (!existingContents.Contains(item.content))
                    {
                        filteredItems.Add(item);
                    }
                }
            }

            return filteredItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "過濾已存在資料時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 載入村里文字巷中英對照資料
    /// </summary>
    private async Task<List<(string content, string type)>> LoadVillageReferenceDataAsync(string filePath)
    {
        try
        {
            var items = new List<(string content, string type)>();
            var lines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 解析格式："中文名稱,""英文名稱"""
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("\"") && trimmedLine.EndsWith("\""))
                {
                    // 移除外層引號
                    trimmedLine = trimmedLine[1..^1];

                    // 找到中間的分隔符 ","" 
                    var separatorIndex = trimmedLine.IndexOf(",\"\"");
                    if (separatorIndex > 0)
                    {
                        var chineseName = trimmedLine[..separatorIndex].Trim();
                        var englishName = trimmedLine[(separatorIndex + 3)..].TrimEnd('\"').Trim();

                        if (!string.IsNullOrEmpty(chineseName) && !string.IsNullOrEmpty(englishName))
                        {
                            // 建立中英文對照的內容，包含兩種格式便於搜尋
                            var combinedContent = $"{chineseName} | {englishName}";

                            // 根據名稱結尾判斷類型
                            string type = "Village"; // 預設為村里
                            if (chineseName.EndsWith("里"))
                            {
                                type = "Village";
                            }
                            else if (chineseName.EndsWith("巷") || chineseName.EndsWith("路") || chineseName.EndsWith("街"))
                            {
                                type = "Lane";
                            }

                            items.Add((combinedContent, type));

                            // 同時添加純中文名稱，方便單獨搜尋
                            items.Add((chineseName, type));
                        }
                    }
                }
            }

            _logger.LogInformation("成功載入 {Count} 筆村里文字巷中英對照資料", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入村里文字巷中英對照資料時發生錯誤: {FilePath}", filePath);
            throw;
        }
    }
}
