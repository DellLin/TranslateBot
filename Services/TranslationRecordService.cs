using Microsoft.EntityFrameworkCore;
using TranslateBot.Data;
using TranslateBot.Models;

namespace TranslateBot.Services;

public class TranslationRecordService
{
    private readonly TranslateBotDbContext _context;

    public TranslationRecordService(TranslateBotDbContext context)
    {
        _context = context;
    }

    public async Task<TranslationRecord> RecordTranslationAsync(
        string originalText,
        string translatedText,
        string translationType,
        TimeSpan processingTime,
        string? userSession = null)
    {
        var record = new TranslationRecord
        {
            OriginalText = originalText,
            TranslatedText = translatedText,
            TranslationType = translationType,
            UserSession = userSession,
            ProcessingTime = processingTime,
            IsSuccessful = true
        };

        _context.TranslationRecords.Add(record);
        await _context.SaveChangesAsync();

        return record;
    }

    public async Task<TranslationRecord> RecordTranslationErrorAsync(
        string originalText,
        string translationType,
        string errorMessage,
        TimeSpan processingTime,
        string? userSession = null)
    {
        var record = new TranslationRecord
        {
            OriginalText = originalText,
            TranslatedText = string.Empty,
            TranslationType = translationType,
            UserSession = userSession,
            ProcessingTime = processingTime,
            IsSuccessful = false,
            ErrorMessage = errorMessage
        };

        _context.TranslationRecords.Add(record);
        await _context.SaveChangesAsync();

        return record;
    }

    public async Task<IEnumerable<TranslationRecord>> GetTranslationHistoryAsync(
        int page = 1,
        int pageSize = 20,
        string? translationType = null,
        string? userSession = null)
    {
        var query = _context.TranslationRecords.AsQueryable();

        if (!string.IsNullOrEmpty(translationType))
        {
            query = query.Where(r => r.TranslationType == translationType);
        }

        if (!string.IsNullOrEmpty(userSession))
        {
            query = query.Where(r => r.UserSession == userSession);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalRecordsCountAsync(
        string? translationType = null,
        string? userSession = null)
    {
        var query = _context.TranslationRecords.AsQueryable();

        if (!string.IsNullOrEmpty(translationType))
        {
            query = query.Where(r => r.TranslationType == translationType);
        }

        if (!string.IsNullOrEmpty(userSession))
        {
            query = query.Where(r => r.UserSession == userSession);
        }

        return await query.CountAsync();
    }

    public async Task<TranslationRecord?> GetTranslationRecordAsync(int id)
    {
        return await _context.TranslationRecords.FindAsync(id);
    }

    public async Task<IEnumerable<TranslationRecord>> SearchTranslationsAsync(
        string searchText,
        int page = 1,
        int pageSize = 20)
    {
        return await _context.TranslationRecords
            .Where(r => r.OriginalText.Contains(searchText) ||
                       r.TranslatedText.Contains(searchText))
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Dictionary<string, object>> GetTranslationStatisticsAsync()
    {
        var totalRecords = await _context.TranslationRecords.CountAsync();
        var successfulTranslations = await _context.TranslationRecords
            .Where(r => r.IsSuccessful)
            .CountAsync();
        var addressTranslations = await _context.TranslationRecords
            .Where(r => r.TranslationType == "Address")
            .CountAsync();
        var productTranslations = await _context.TranslationRecords
            .Where(r => r.TranslationType == "Product")
            .CountAsync();

        var averageProcessingTime = await _context.TranslationRecords
            .Where(r => r.IsSuccessful)
            .AverageAsync(r => r.ProcessingTime.TotalMilliseconds);

        return new Dictionary<string, object>
        {
            ["totalRecords"] = totalRecords,
            ["successfulTranslations"] = successfulTranslations,
            ["failedTranslations"] = totalRecords - successfulTranslations,
            ["addressTranslations"] = addressTranslations,
            ["productTranslations"] = productTranslations,
            ["averageProcessingTimeMs"] = averageProcessingTime,
            ["successRate"] = totalRecords > 0 ? (double)successfulTranslations / totalRecords * 100 : 0
        };
    }
}
