using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace TranslateBot.Services;

public class EmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(Kernel kernel, ILogger<EmbeddingService> logger)
    {
        _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        _logger = logger;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        return await GenerateEmbeddingWithRetryAsync(text);
    }

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts)
    {
        try
        {
            var results = new List<ReadOnlyMemory<float>>();
            foreach (var text in texts)
            {
                var result = await GenerateEmbeddingWithRetryAsync(text);
                results.Add(result);
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量生成 Embedding 時發生錯誤");
            throw;
        }
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingWithRetryAsync(string text, int maxRetries = 3)
    {
        int retryCount = 0;

        while (retryCount <= maxRetries)
        {
            try
            {
                var result = await _embeddingGenerator.GenerateAsync(text);
                return result.Vector;
            }
            catch (Exception ex) when (Is429Error(ex) && retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning("生成 Embedding 時遇到 429 錯誤，第 {RetryCount} 次重試，等待 30 秒...", retryCount);
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成 Embedding 時發生錯誤: {Text}", text);
                throw;
            }
        }

        throw new InvalidOperationException($"生成 Embedding 在 {maxRetries} 次重試後仍然失敗: {text}");
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

    public static double CosineSimilarity(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
    {
        var span1 = embedding1.Span;
        var span2 = embedding2.Span;

        if (span1.Length != span2.Length)
            throw new ArgumentException("Embeddings must have the same length");

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < span1.Length; i++)
        {
            dotProduct += span1[i] * span2[i];
            magnitude1 += span1[i] * span1[i];
            magnitude2 += span2[i] * span2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}
