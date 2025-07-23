using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace TranslateBot.Services;

public class TranslationService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly RagService _ragService;
    private readonly CsvReaderService _csvReaderService;
    private readonly TranslationRecordService _recordService;

    public TranslationService(IChatCompletionService chatCompletionService, RagService ragService, CsvReaderService csvReaderService, TranslationRecordService recordService)
    {
        _chatCompletionService = chatCompletionService;
        _ragService = ragService;
        _csvReaderService = csvReaderService;
        _recordService = recordService;
    }

    public async Task<string> TranslateAddressAsync(string address, string? userSession = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 使用 RAG 取得相關參考資料
            string ragContext = "";
            try
            {
                ragContext = await _ragService.GetRelevantContextAsync(address);
            }
            catch (Exception ex)
            {
                // 如果 RAG 失敗，回退到原來的 CSV 讀取方式
                Console.WriteLine($"Warning: RAG service failed, falling back to CSV: {ex.Message}");

                try
                {
                    var streetNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "街路名稱.csv");
                    var countyNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "縣市名稱.csv");
                    ragContext = _csvReaderService.ReadCsvFiles(streetNamesPath, countyNamesPath);
                }
                catch (Exception csvEx)
                {
                    Console.WriteLine($"Warning: Could not read CSV files: {csvEx.Message}");
                }
            }

            // 建立一個新的聊天歷史記錄，但包含系統訊息
            var tempHistory = new ChatHistory();

            var systemMessage = """
        你是一個專門處理台灣地址翻譯的程式。

        你的唯一功能是：
        ---
        **將所有外文的台灣地址翻譯成繁體中文。**
        ---

        **翻譯規則：**
        * 請確保翻譯後的地址符合台灣地址的書寫習慣和順序。
        * 任何外文地址，無論是英文、日文或其他語言，都應精準地翻譯為繁體中文。
        * 即使地址中有部分為數字或專有名詞，也請盡力找到對應的繁體中文表達。

        **重要限制：**
        * **除了地址翻譯，你不會回答任何其他問題。**
        * 如果使用者輸入的內容不是台灣地址，或者涉及其他主題，你應該禮貌地拒絕並重申你的唯一功能。
        * 你不會進行對話、解釋、提供建議或執行任何與地址翻譯無關的任務。

        **範例輸入：**
        * "No. 7, Sec. 5, Xinyi Rd., Xinyi Dist., Taipei City"
        * "10, Da'an Rd., Sec. 1, Da'an Dist., Taipei City"
        * "台中市西屯區台灣大道三段301號" (此為中文地址，不需要翻譯，可直接回覆確認)

        **範例輸出：**
        * "台北市信義區信義路五段7號"
        * "台北市大安區大安路一段10號"
        * "台中市西屯區台灣大道三段301號" (或 "這已經是繁體中文台灣地址，無需翻譯。")

        請記住你的核心任務：**只翻譯台灣外文地址為繁體中文，拒絕一切其他請求。**
        """;

            // 如果成功讀取到參考資料，則將其附加到系統訊息中
            if (!string.IsNullOrWhiteSpace(ragContext))
            {
                systemMessage += $"\n\n**參考資料：**\n{ragContext}";
            }

            tempHistory.AddSystemMessage(systemMessage);
            tempHistory.AddUserMessage(address);

            // 從 AI 取得回應
            var result = await _chatCompletionService.GetChatMessageContentAsync(tempHistory);
            var translatedText = result.Content ?? string.Empty;

            stopwatch.Stop();

            // 記錄成功的翻譯
            await _recordService.RecordTranslationAsync(
                address,
                translatedText,
                "Address",
                stopwatch.Elapsed,
                userSession);

            return translatedText;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 記錄失敗的翻譯
            await _recordService.RecordTranslationErrorAsync(
                address,
                "Address",
                ex.Message,
                stopwatch.Elapsed,
                userSession);

            throw; // 重新拋出異常讓控制器處理
        }
    }

    public async Task<string> TranslateProductNameAsync(string productName, string? userSession = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 建立商品名稱翻譯的聊天歷史記錄
            var tempHistory = new ChatHistory();

            var systemMessage = """
        你是一個專門識別商品種類的專業助手。

        你的功能是：
        ---
        **根據商品名稱判斷商品種類，並回復簡短的繁體中文商品種類名稱。**
        ---

        **判斷規則：**
        * 分析商品名稱中的關鍵字和特徵，判斷商品屬於哪個種類
        * 回復最準確、最簡潔的商品種類名稱
        * 使用台灣常用的商品分類術語
        * 只回復商品種類，不包含品牌名稱或型號

        **範例：**
        * "iPhone 15 Pro Max" → "手機"
        * "Wireless Bluetooth Headphones" → "藍芽耳機"
        * "Samsung Galaxy S24" → "手機"
        * "Organic Green Tea" → "綠茶"
        * "Nike Air Jordan" → "籃球鞋"
        * "AirPods Pro" → "藍芽耳機"
        * "MacBook Pro" → "筆記型電腦"
        * "PlayStation 5" → "遊戲主機"
        * "Canon EOS R5" → "數位相機"
        * "Instant Coffee" → "即溶咖啡"

        請只回應簡短的繁體中文商品種類名稱，不需要額外解釋或描述。
        """;

            tempHistory.AddSystemMessage(systemMessage);
            tempHistory.AddUserMessage(productName);

            // 從 AI 取得回應
            var result = await _chatCompletionService.GetChatMessageContentAsync(tempHistory);
            var translatedText = result.Content ?? string.Empty;

            stopwatch.Stop();

            // 記錄成功的翻譯
            await _recordService.RecordTranslationAsync(
                productName,
                translatedText,
                "Product",
                stopwatch.Elapsed,
                userSession);

            return translatedText;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 記錄失敗的翻譯
            await _recordService.RecordTranslationErrorAsync(
                productName,
                "Product",
                ex.Message,
                stopwatch.Elapsed,
                userSession);

            throw; // 重新拋出異常讓控制器處理
        }
    }
}
