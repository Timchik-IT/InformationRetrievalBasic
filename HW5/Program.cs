using System.Text;
using Common;

namespace HW5;

class Program
{
    // Путь к результатам HW4 (tf-idf по документам)
    private static readonly string Hw4BasePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW4\lemmas_terms_per_doc"));

    // Векторы документов: docName -> term -> weight (tf-idf)
    private static readonly Dictionary<string, Dictionary<string, double>> DocTermVectors =
        new(StringComparer.OrdinalIgnoreCase);

    static void Main(string[] args)
    {
        Console.WriteLine("=== Векторный поиск (HW5) ===\n");

        if (!Directory.Exists(Hw4BasePath))
        {
            Console.WriteLine($"Ошибка: не найдена папка с результатами HW4: '{Hw4BasePath}'.");
            Console.WriteLine("Сначала запустите HW4, чтобы посчитать tf-idf.");
            return;
        }

        // 1. Загружаем векторы документов из файлов *_terms.txt (можно аналогично добавить *_lemmas.txt)
        LoadDocumentVectors();

        if (DocTermVectors.Count == 0)
        {
            Console.WriteLine("Не удалось загрузить ни одного документа.");
            return;
        }

        // 2. Интерактивный цикл векторного поиска
        RunSearchLoop();
    }

    /// <summary>
    /// Загружает TF-IDF вектора документов из файлов HW4.
    /// Ожидается формат строк: <term> <idf> <tf-idf>
    /// </summary>
    private static void LoadDocumentVectors()
    {
        var termFiles = Directory.GetFiles(Hw4BasePath, "*_terms.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in termFiles)
        {
            var docName = Path.GetFileNameWithoutExtension(file).Replace("_terms", ".txt");
            var vector = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {
                var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                var term = parts[0];

                // TF-IDF в файлах HW4 записан с запятой как десятичным разделителем (текущая культура).
                if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.CurrentCulture, out var tfidf))
                    continue;

                vector[term] = tfidf;
            }

            if (vector.Count > 0)
                DocTermVectors[docName] = vector;
        }

        Console.WriteLine($"Загружено векторов документов: {DocTermVectors.Count}\n");
    }

    /// <summary>
    /// Интерактивный цикл поиска по векторной модели (косинусное сходство).
    /// </summary>
    private static void RunSearchLoop()
    {
        Console.WriteLine("Введите текстовый запрос (для выхода: exit):\n");

        while (true)
        {
            Console.Write("Запрос > ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(query) ||
                query.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            var results = Search(query);
            PrintResults(results);
        }
    }

    /// <summary>
    /// Векторный поиск: строим вектор запроса и считаем косинусное сходство с каждым документом.
    /// </summary>
    private static List<(string DocName, double Score)> Search(string query)
    {
        // 1. Токенизируем запрос так же, как документы
        var queryTokens = TokenizeQueryText(query);
        if (queryTokens.Count == 0)
            return [];

        // 2. Строим вектор запроса: term -> tf (можно было бы умножать на idf, но
        // для простоты используем чистый tf, так как веса документов уже tf-idf).
        var termCounts = queryTokens.GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var totalTerms = queryTokens.Count;
        var queryVector = termCounts.ToDictionary(
            kvp => kvp.Key,
            kvp => (double)kvp.Value / totalTerms,
            StringComparer.OrdinalIgnoreCase);

        // 3. Считаем косинусное сходство между вектором запроса и каждым документом
        var results = new List<(string DocName, double Score)>();

        foreach (var (docName, docVector) in DocTermVectors)
        {
            var score = CosineSimilarity(queryVector, docVector);
            if (score > 0)
                results.Add((docName, score));
        }

        // 4. Сортируем по убыванию сходства
        return results
            .OrderByDescending(r => r.Score)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Токенизация текста запроса (та же логика, что и для документов).
    /// </summary>
    private static List<string> TokenizeQueryText(string query)
    {
        // Используем ту же регулярку и стоп-слова, что и в TextProcessor.Tokenize
        return TextProcessor.Tokenize(query, minTokenLength: 3).ToList();
    }

    /// <summary>
    /// Косинусное сходство между двумя векторами (term -> weight).
    /// </summary>
    private static double CosineSimilarity(
        Dictionary<string, double> v1,
        Dictionary<string, double> v2)
    {
        double dot = 0;
        double norm1 = 0;
        double norm2 = 0;

        foreach (var (term, w1) in v1)
        {
            norm1 += w1 * w1;
            if (v2.TryGetValue(term, out var w2))
                dot += w1 * w2;
        }

        foreach (var w2 in v2.Values)
            norm2 += w2 * w2;

        if (norm1 == 0 || norm2 == 0)
            return 0;

        return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    /// <summary>
    /// Печать результатов векторного поиска.
    /// </summary>
    private static void PrintResults(List<(string DocName, double Score)> results)
    {
        if (results.Count == 0)
        {
            Console.WriteLine("Ничего не найдено.\n");
            return;
        }

        Console.WriteLine($"Найдено документов: {results.Count}");
        foreach (var (docName, score) in results)
        {
            Console.WriteLine($"  {docName}  (score = {score:F4})");
        }

        Console.WriteLine();
    }
}