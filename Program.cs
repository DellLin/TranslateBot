// 匯入套件
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.EntityFrameworkCore;
using TranslateBot.Services;
using TranslateBot.Data;
using Pgvector.EntityFrameworkCore;

// 載入 .env 檔案
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 從環境變數讀取設定值
var modelId = Environment.GetEnvironmentVariable("GEMINI_MODEL_ID")
    ?? throw new InvalidOperationException("環境變數 GEMINI_MODEL_ID 未設定");
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("環境變數 GEMINI_API_KEY 未設定");
var embeddingModelId = Environment.GetEnvironmentVariable("GEMINI_EMBEDDING_MODEL_ID")
    ?? "models/text-embedding-004"; // Google 的 embedding 模型
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=translatebot;Username=postgres;Password=password";

// 配置資料庫
builder.Services.AddDbContext<TranslateBotDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// 使用 Gemini API 聊天完成建立核心
var kernelBuilder = Kernel.CreateBuilder()
    .AddGoogleAIGeminiChatCompletion(modelId, apiKey)
    .AddGoogleAIEmbeddingGenerator(embeddingModelId, apiKey, dimensions: 768);

// 建置核心
Kernel kernel = kernelBuilder.Build();

// 註冊服務
builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton<IChatCompletionService>(provider =>
    provider.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
builder.Services.AddScoped<EmbeddingService>(provider =>
    new EmbeddingService(provider.GetRequiredService<Kernel>(), provider.GetRequiredService<ILogger<EmbeddingService>>()));
builder.Services.AddScoped<RagService>();
builder.Services.AddSingleton<CsvReaderService>();
builder.Services.AddScoped<TranslationService>();

// 加入控制器支援
builder.Services.AddControllers();

// 加入 CORS 支援
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 加入 API Explorer 和 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 初始化資料庫
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TranslateBotDbContext>();
    var ragService = scope.ServiceProvider.GetRequiredService<RagService>();
    var csvReaderService = scope.ServiceProvider.GetRequiredService<CsvReaderService>();

    try
    {
        // 確保資料庫被創建
        await dbContext.Database.EnsureCreatedAsync();

        // 啟用 pgvector 擴展 (如果尚未啟用)
        await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");

        // 初始化向量資料
        var streetNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "街路名稱.csv");
        var countyNamesPath = Path.Combine(Directory.GetCurrentDirectory(), "縣市名稱.csv");
        var villageReferencePath = Path.Combine(Directory.GetCurrentDirectory(), "村里文字巷中英對照.TXT");

        // await ragService.InitializeDataAsync(csvReaderService, streetNamesPath, countyNamesPath, villageReferencePath);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "初始化資料庫時發生錯誤");
    }
}

// 設定 HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 啟用 CORS
app.UseCors();

app.UseAuthorization();

// 設定默認路由，讓根路徑顯示 index.html
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();