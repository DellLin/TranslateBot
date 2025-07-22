# TranslateBot - 智慧翻譯系統

一個基於 Microsoft Semantic Kernel 和 Google Gemini AI 的智慧翻譯服務，支援台灣地址和商品名稱的中英對照翻譯。

## 功能特色

### 🏠 地址翻譯

- 整合中華郵政全球資訊網完整地名資料庫
- 支援縣市、街路、村里等各級地址的精準翻譯
- 運用 RAG（檢索增強生成）技術提供智慧化翻譯建議
- 支援從英文地址翻譯為繁體中文台灣地址

### 📦 商品名稱翻譯

- 支援多國語言商品名稱翻譯為繁體中文
- 保持品牌名稱的正確性和一致性
- 適用於各種產品類型：電子產品、服飾、食品等
- 符合台灣市場的中文表達習慣

## API 端點

### 地址翻譯

```
POST /api/translate/address
Content-Type: application/json

{
    "address": "No. 1, Shifu Rd., Xinyi Dist., Taipei City 110204"
}
```

**回應：**

```json
{
  "originalAddress": "No. 1, Shifu Rd., Xinyi Dist., Taipei City 110204",
  "translatedAddress": "台北市信義區市府路1號"
}
```

### 商品名稱翻譯

```
POST /api/translate/product
Content-Type: application/json

{
    "productName": "Wireless Bluetooth Headphones"
}
```

**回應：**

```json
{
  "originalProductName": "Wireless Bluetooth Headphones",
  "translatedProductName": "無線藍牙耳機"
}
```

## 技術架構

- **框架**: ASP.NET Core
- **AI 整合**: Microsoft Semantic Kernel
- **語言模型**: Google Gemini AI
- **資料存取**: Entity Framework Core
- **嵌入服務**: 用於 RAG 功能的語意搜尋
- **前端**: HTML5 + CSS3 + JavaScript

## 快速開始

1. Clone 專案

```bash
git clone https://github.com/DellLin/TranslateBot.git
cd TranslateBot
```

2. 設置環境變數

創建 `.env` 檔案並設置必要的配置：

```bash
# Google AI 設定
GEMINI_MODEL_ID=gemini-2.5-flash
GEMINI_API_KEY=your-google-ai-api-key-here
GEMINI_EMBEDDING_MODEL_ID=gemini-embedding-001

# 資料庫連接字串
DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=translatebot;Username=postgres;Password=your-password-here
```

3. 初始化資料庫

確保 PostgreSQL 服務正在運行，然後使用專案提供的 `Database/init.sql` 腳本進行完整的資料庫初始化：

```bash
# 使用 PostgreSQL 連接並執行初始化腳本
```

這個腳本會自動執行以下操作：

- 創建 `translatebot` 資料庫
- 啟用 `pgvector` 擴展（用於向量相似性搜索）
- 創建 `address_references` 資料表（地址參考資料）
- 創建 `translation_records` 資料表（翻譯記錄）
- 建立必要的索引（提升查詢效能）

**故障排除：**

- 如果遇到權限問題，請確保 PostgreSQL 用戶有創建資料庫的權限
- 如果 `pgvector` 擴展安裝失敗，請先安裝 pgvector：

> **重要**: 使用 `init.sql` 腳本是推薦的初始化方式，它包含完整的資料庫結構和索引配置。

4. 運行專案

```bash
dotnet run
```

5. 在瀏覽器中開啟 `http://localhost:5000` 或 `https://localhost:5001`

## 資料來源

- 縣市名稱對照：中華郵政全球資訊網-縣市鄉鎮中英對照
- 街路名稱對照：中華郵政全球資訊網-街路名稱中英對照
- 村里文字巷對照：中華郵政全球資訊網-村里文字巷中英對照

## 授權

Copyright © 2025 TranslateBot. All rights reserved.
