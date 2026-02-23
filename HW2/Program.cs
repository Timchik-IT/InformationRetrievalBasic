using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using opennlp.tools.stemmer;

namespace HW2;

class Program
{
    // Пути к данным и результатам
    private static readonly string DataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HW1\crawled_pages"));
    private static readonly string OutputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
    private static readonly string TokensPath = Path.Combine(OutputDir, "tokens.txt");
    private static readonly string LemmasPath = Path.Combine(OutputDir, "lemmas.txt");

    // Стоп-слова для фильтрации (предлоги, союзы, местоимения, глаголы-связки)
    private static readonly HashSet<string> StopWords =
    [
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "up", "about", "into", "through", "during", "before", "after", "above", "below", "between", "both",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "can", "may", "might", "must", "i", "you", "he", "she", "it",
        "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "its", "our", "their"
    ];

    // Стеммер Портера для приведения слов к основе
    private static readonly PorterStemmer Stemmer = new();

    static void Main(string[] args)
    {
        // Основной пайплайн обработки
        var tokens = ProcessFiles();
        var lemmas = GroupTokensByStem(tokens);

        // Сохранение результатов
        SaveResults(tokens, lemmas);

        Console.WriteLine($"Готово! Токенов: {tokens.Count}, Групп (лемм): {lemmas.Count}");
    }

    /// <summary>
    /// Основной метод обработки: читает файлы, извлекает текст, токенизирует и фильтрует
    /// </summary>
    /// <returns>HashSet уникальных валидных токенов</returns>
    private static HashSet<string> ProcessFiles()
    {
        // Читаем все HTML-файлы и извлекаем чистый текст
        var texts = Directory.GetFiles(DataPath, "*.txt")
            .Select(ExtractTextFromHtml);

        // Объединяем весь текст в одну строку для токенизации
        var cleanText = string.Join("\n", texts);

        // Токенизация: извлекаем только слова из латинских букв, приводим к нижнему регистру
        var tokens = Regex.Matches(cleanText.ToLower(), @"[a-z]+")
            .Select(m => m.Value)
            // Фильтруем: длина > 1 и не стоп-слово
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .ToHashSet();

        return tokens;
    }

    /// <summary>
    /// Извлекает текстовое содержимое из HTML-файла, удаляя все теги
    /// </summary>
    /// <param name="filePath">Путь к HTML-файлу</param>
    /// <returns>Чистый текст без разметки</returns>
    private static string ExtractTextFromHtml(string filePath)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(File.ReadAllText(filePath, Encoding.UTF8));

        // Внутренний текст узла содержит только видимый контент без тегов
        return doc.DocumentNode.InnerText;
    }

    /// <summary>
    /// Группирует токены по их стеммам (основам слов)
    /// </summary>
    /// <param name="tokens">Набор токенов для группировки</param>
    /// <returns>Словарь: лемма -> список исходных токенов</returns>
    private static Dictionary<string, List<string>> GroupTokensByStem(HashSet<string> tokens)
    {
        return tokens
            // Группируем по стемму каждого токена
            .GroupBy(t => Stemmer.stem(t))
            // Фильтруем пустые ключи и стоп-слова среди лемм
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && !StopWords.Contains(g.Key))
            // Создаём словарь с игнорированием регистра для ключей
            .ToDictionary(
                g => g.Key,
                g => g.ToList(),
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <summary>
    /// Сохраняет результаты обработки в файлы
    /// </summary>
    /// <param name="tokens">Набор уникальных токенов</param>
    /// <param name="lemmas">Словарь лемм с группами токенов</param>
    private static void SaveResults(HashSet<string> tokens, Dictionary<string, List<string>> lemmas)
    {
        // Сохраняем токены: по одному в строке, отсортированные
        File.WriteAllLines(
            TokensPath,
            tokens.OrderBy(t => t),
            Encoding.UTF8
        );

        // Сохраняем леммы в формате: <лемма> <токен1> <токен2> ... <токенN>
        var lemmaLines = lemmas
            .Select(g => $"{g.Key} {string.Join(" ", g.Value.OrderBy(t => t))}")
            .OrderBy(l => l);

        File.WriteAllLines(LemmasPath, lemmaLines, Encoding.UTF8);
    }
}