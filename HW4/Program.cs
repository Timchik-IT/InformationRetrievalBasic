using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace HW4;

class Program
{
    private static readonly string DataPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW1\crawled_pages")); // Исходные HTML-файлы

    private static readonly string TokensAndLemmasPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW2")); // Исходные HTML-файлы
    private static readonly string TokensFile = Path.Combine(TokensAndLemmasPath, "tokens.txt"); // Из Задания 2
    private static readonly string LemmasFile = Path.Combine(TokensAndLemmasPath, "lemmas.txt"); // Из Задания 2

    private static readonly string OutputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));

    // Глобальная статистика для IDF
    private static readonly Dictionary<string, int> TermDf = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> LemmaDf = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> TermToLemma = new(StringComparer.OrdinalIgnoreCase);
    private static int _totalDocs;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Расчёт TF-IDF (используем существующие файлы) ===\n");

        // 1. Загружаем маппинг термин -> лемма из lemmas.txt
        LoadLemmasMapping();

        // 2. Считаем DF (document frequency) для IDF — нужно пройтись по всем документам
        Console.WriteLine("Шаг 1: Сбор статистики DF для IDF...");
        CollectDocumentFrequencies();

        // 3. Считаем TF-IDF для каждого документа
        Console.WriteLine("Шаг 2: Расчёт TF-IDF...");
        CalculateTfIdfPerDocument();

        Console.WriteLine("Готово!");
    }

    /// <summary>
    /// Загружает маппинг термин -> лемма из lemmas.txt
    /// Формат: <лемма> <токен1> <токен2> ... <токенN>
    /// </summary>
    private static void LoadLemmasMapping()
    {
        if (!File.Exists(LemmasFile))
        {
            Console.WriteLine($"Файл '{LemmasFile}' не найден. Лемматизация отключена.");
            return;
        }

        foreach (var line in File.ReadAllLines(LemmasFile, Encoding.UTF8))
        {
            var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var lemma = parts[0].ToLower();
            for (var i = 1; i < parts.Length; i++)
            {
                var term = parts[i].ToLower();
                TermToLemma.TryAdd(term, lemma);
            }
        }

        Console.WriteLine($"Загружено маппингов: {TermToLemma.Count}");
    }

    /// <summary>
    /// Считает DF (в скольких документах встречается термин/лемма)
    /// </summary>
    private static void CollectDocumentFrequencies()
    {
        var files = Directory.GetFiles(DataPath, "*.txt").ToList();
        _totalDocs = files.Count;

        foreach (var file in files)
        {
            var terms = ExtractTermsFromFile(file);
            var uniqueTerms = new HashSet<string>(terms, StringComparer.OrdinalIgnoreCase);
            var uniqueLemmas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var term in uniqueTerms)
            {
                TermDf.TryAdd(term, 0);
                TermDf[term]++;

                if (TermToLemma.TryGetValue(term, out var lemma))
                    uniqueLemmas.Add(lemma);
            }

            foreach (var lemma in uniqueLemmas)
            {
                LemmaDf.TryAdd(lemma, 0);
                LemmaDf[lemma]++;
            }
        }

        Console.WriteLine($"Обработано документов: {_totalDocs}");
    }

    /// <summary>
    /// Извлекает термины из файла (используем ту же логику, что в Задании 2)
    /// </summary>
    private static List<string> ExtractTermsFromFile(string filePath)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(File.ReadAllText(filePath, Encoding.UTF8));
        var text = doc.DocumentNode.InnerText;

        return Regex.Matches(text.ToLower(), @"[a-z]+")
            .Select(m => m.Value)
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .ToList();
    }

    /// <summary>
    /// Стоп-слова (должны совпадать с Заданием 2)
    /// </summary>
    private static readonly HashSet<string> StopWords =
    [
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "up", "about", "into", "through", "during", "before", "after", "above", "below", "between", "both",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "can", "may", "might", "must", "i", "you", "he", "she", "it",
        "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "its", "our", "their"
    ];

    /// <summary>
    /// Расчёт TF-IDF для каждого документа
    /// </summary>
    private static void CalculateTfIdfPerDocument()
    {
        var files = Directory.GetFiles(DataPath, "*.txt").ToList();

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var terms = ExtractTermsFromFile(file);
            var totalTerms = terms.Count;
            if (totalTerms == 0) continue;

            // Частота терминов в документе
            var termFreq = terms.GroupBy(t => t)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            // === Для терминов ===
            var termLines = new List<string>();
            foreach (var (term, count) in termFreq)
            {
                var tf = (double)count / totalTerms;
                var idf = CalculateIdf(term, TermDf);
                var tfidf = tf * idf;

                termLines.Add($"{term} {idf:F6} {tfidf:F6}");
            }

            // Сохраняем для терминов
            File.WriteAllLines(
                Path.Combine(OutputDir, $"{fileName}_terms.txt"),
                termLines.OrderBy(l => l.Split(' ')[0]),
                Encoding.UTF8
            );

            // === Для лемм ===
            var lemmaFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in termFreq)
            {
                if (TermToLemma.TryGetValue(kvp.Key, out var lemma))
                {
                    lemmaFreq.TryAdd(lemma, 0);
                    lemmaFreq[lemma] += kvp.Value; // сумма вхождений всех терминов этой леммы
                }
            }

            var lemmaLines = new List<string>();
            foreach (var (lemma, count) in lemmaFreq)
            {
                var tf = (double)count / totalTerms;
                var idf = CalculateIdf(lemma, LemmaDf);
                var tfidf = tf * idf;

                lemmaLines.Add($"{lemma} {idf:F6} {tfidf:F6}");
            }

            // Сохраняем для лемм
            File.WriteAllLines(
                Path.Combine(OutputDir, $"{fileName}_lemmas.txt"),
                lemmaLines.OrderBy(l => l.Split(' ')[0]),
                Encoding.UTF8
            );
        }
    }

    /// <summary>
    /// Формула IDF: log(N / (1 + df))
    /// </summary>
    private static double CalculateIdf(string term, Dictionary<string, int> dfMap)
    {
        if (!dfMap.TryGetValue(term, out var df)) return 0;
        return Math.Log(_totalDocs / (1.0 + df));
    }
}