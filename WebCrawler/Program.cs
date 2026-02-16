using System.Text.Json;
using System.Net.Http;
using HtmlAgilityPack;

namespace WebCrawler;

class Program
{
    private static readonly HttpClient Client = CreateHttpClient();
    private const string UrlsFile = "../../../urls.json";
    private const string IndexPath = "../../../index.txt";
    private const string OutputDir = "../../../crawled_pages";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting crawler...");

        if (!Directory.Exists(OutputDir))
            Directory.CreateDirectory(OutputDir);

        List<string> urls;
        try
        {
            urls = LoadUrls(UrlsFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load URLs: {ex.Message}");
            return;
        }

        List<string> indexLines = [];
        var fileIndex = 1;
        var maxPages = 100;

        foreach (var url in urls)
        {
            if (fileIndex > maxPages) break;

            Console.WriteLine($"[{fileIndex}/{maxPages}] Downloading {url}");

            try
            {
                // 1. Скачиваем HTML
                var html = await Client.GetStringAsync(url);

                // 2. Очищаем от скриптов и стилей (но оставляем верстку)
                var cleanedHtml = CleanHtml(html);

                // 3. Сохраняем именно очищенную версию (cleanedHtml), а не оригинал
                var filePath = Path.Combine(OutputDir, $"{fileIndex}.txt");
                await File.WriteAllTextAsync(filePath, cleanedHtml);

                // 4. Пишем в индекс
                indexLines.Add($"{fileIndex}\t{url}");

                fileIndex++;
                await Task.Delay(50); // Небольшая пауза
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on {url}: {ex.Message}");
            }
        }

        await File.WriteAllLinesAsync(IndexPath, indexLines);
        Console.WriteLine($"Crawling completed. {fileIndex - 1} pages saved.");
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        return httpClient;
    }

    private static List<string> LoadUrls(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("urls", out var urlsElement))
            throw new Exception("JSON must contain 'urls' property");

        return urlsElement.EnumerateArray()
            .Select(item => item.GetString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Удаляет script, style, link, но оставляет HTML структуру (div, p, h1...)
    /// </summary>
    private static string CleanHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Теги, которые нужно вырезать полностью
        var tagsToRemove = new[] { "script", "style", "link", "noscript", "iframe", "meta" };

        foreach (var tagName in tagsToRemove)
        {
            var nodes = doc.DocumentNode.Descendants(tagName).ToList();
            foreach (var node in nodes)
            {
                node.Remove();
            }
        }

        // Получаем HTML
        var cleanedHtml = doc.DocumentNode.OuterHtml;

        // Удаляем пустые строки и строки только с пробелами/табами
        var lines = cleanedHtml.Split('\n');
        var nonEmptyLines = lines
            .Select(l => l.TrimEnd('\r')) // Убираем \r
            .Where(l => !string.IsNullOrWhiteSpace(l)) // Фильтруем пустые
            .ToArray();

        return string.Join("\n", nonEmptyLines);
    }
}