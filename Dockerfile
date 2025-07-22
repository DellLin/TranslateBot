# 使用 Microsoft 官方 .NET 8.0 SDK 映像作為建置階段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# 複製 csproj 檔案並還原相依套件
COPY *.csproj ./
RUN dotnet restore

# 複製其餘檔案並建置應用程式
COPY . ./
RUN dotnet publish -c Release -o out

# 建置執行階段映像
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 複製建置輸出
COPY --from=build-env /app/out .

# 建立非 root 使用者
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# 暴露埠號
EXPOSE 8080

# 設定環境變數
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 啟動應用程式
ENTRYPOINT ["dotnet", "TranslateBot.dll"]
