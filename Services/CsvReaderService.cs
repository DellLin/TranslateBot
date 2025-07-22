using System.Text;

namespace TranslateBot.Services;

public class CsvReaderService
{
    public string ReadCsvFiles(string streetNamesPath, string countyNamesPath)
    {
        try
        {
            var content = new StringBuilder();

            // 讀取街路名稱 CSV
            if (File.Exists(streetNamesPath))
            {
                content.AppendLine("**街路名稱參考資料：**");
                var streetNames = File.ReadAllLines(streetNamesPath, Encoding.UTF8);
                foreach (var line in streetNames)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        content.AppendLine(line);
                    }
                }
                content.AppendLine();
            }

            // 讀取縣市名稱 CSV
            if (File.Exists(countyNamesPath))
            {
                content.AppendLine("**縣市名稱參考資料：**");
                var countyNames = File.ReadAllLines(countyNamesPath, Encoding.UTF8);
                foreach (var line in countyNames)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        content.AppendLine(line);
                    }
                }
            }

            return content.ToString().Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read CSV files: {ex.Message}", ex);
        }
    }
}
