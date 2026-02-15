using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebCrawlerHomework
{
    class Program
    {
        private static readonly HttpClient client;

        static Program()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        static async Task Main(string[] args)
        {
            string urlsFile = "/home/sketch/source/InformationRetrievalBasic/WebCrawler/urls.json";
            string indexPath = "/home/sketch/source/InformationRetrievalBasic/WebCrawler/index.txt";
            string outputDir = "/home/sketch/source/InformationRetrievalBasic/WebCrawler/crawled_pages";

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var urls = LoadUrlsFromFile(urlsFile);
            int fileIndex = 1;
            List<string> indexLines = new List<string>();

            foreach (var url in urls)
            {
                Console.WriteLine($"Processing {url}...");
                try
                {
                    string content = await client.GetStringAsync(url);
                    string fileName = Path.Combine(outputDir, $"{fileIndex}.txt");
                    await File.WriteAllTextAsync(fileName, content);
                    indexLines.Add($"{fileIndex}\t{url}");
                    fileIndex++;
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP Error downloading {url}: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download {url}: {ex.Message}");
                }

                if (fileIndex > 100) break;
            }

            await File.WriteAllLinesAsync(indexPath, indexLines);
            Console.WriteLine($"Crawling finished. Index saved to {indexPath}.");
        }

        static List<string> LoadUrlsFromFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            string jsonContent = reader.ReadToEnd();
            var jsonData = System.Text.Json.JsonDocument.Parse(jsonContent);
            var urlsArray = jsonData.RootElement.GetProperty("urls");

            List<string> urls = new List<string>();
            foreach (var item in urlsArray.EnumerateArray())
            {
                urls.Add(item.GetString());
            }

            return urls;
        }
    }
}