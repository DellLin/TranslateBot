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

1. 克隆專案

```bash
git clone https://github.com/DellLin/TranslateBot.git
cd TranslateBot
```

2. 設置環境變數

```bash
# 在 .env 檔案中設置必要的 API 金鑰
GOOGLE_API_KEY=your_google_api_key_here
```

3. 運行專案

```bash
dotnet run
```

4. 在瀏覽器中開啟 `https://localhost:7000`

## 資料來源

- 縣市名稱對照：中華郵政全球資訊網-縣市鄉鎮中英對照
- 街路名稱對照：中華郵政全球資訊網-街路名稱中英對照
- 村里文字巷對照：中華郵政全球資訊網-村里文字巷中英對照

## 授權

Copyright © 2025 TranslateBot. All rights reserved.
