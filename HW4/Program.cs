using System.Text;
using Common;

namespace HW4;

class Program
{
    // Исходные HTML-файлы (Задание 1)
    private static readonly string DataPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW1\crawled_pages"));

    // Результаты HW2 (списки терминов и лемм)
    private static readonly string Hw2BasePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW2"));
    private static readonly string LemmasFile = Path.Combine(Hw2BasePath, "lemmas.txt");

    // Пер-документные токены и леммы из HW2
    private static readonly string TokensPerDocPath = Path.Combine(Hw2BasePath, "tokens_per_doc");
    private static readonly string LemmasPerDocPath = Path.Combine(Hw2BasePath, "lemmas_per_doc");

    // Директория, куда HW4 пишет свои результаты
    private static readonly string OutputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\lemmas_terms_per_doc"));

    // Глобальная статистика для IDF
    private static readonly Dictionary<string, int> TermDf = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> LemmaDf = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> TermToLemma = new(StringComparer.OrdinalIgnoreCase);
    private static int _totalDocs;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Расчёт TF-IDF (используем существующие файлы) ===\n");

        if (!Directory.Exists(DataPath))
        {
            Console.WriteLine($"Ошибка: Папка с исходными документами '{DataPath}' не найдена.");
            return;
        }

        if (!Directory.Exists(TokensPerDocPath) || !Directory.Exists(LemmasPerDocPath))
        {
            Console.WriteLine($"Ошибка: Не найдены результаты HW2 (ожидаются '{TokensPerDocPath}' и '{LemmasPerDocPath}').");
            Console.WriteLine("Сначала запустите HW2 для генерации per-doc токенов и лемм.");
            return;
        }

        // Создаём директорию вывода (иначе WriteAllLines упадёт)
        Directory.CreateDirectory(OutputDir);

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
    /// Считает DF (в скольких документах встречается термин/лемма),
    /// используя per-doc токены и леммы из HW2.
    /// </summary>
    private static void CollectDocumentFrequencies()
    {
        // === DF для терминов ===
        var tokenFiles = Directory.GetFiles(TokensPerDocPath, "*_tokens.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _totalDocs = tokenFiles.Count;

        foreach (var file in tokenFiles)
        {
            // В файле по одному уникальному термину в строке
            var uniqueTerms = File.ReadAllLines(file, Encoding.UTF8)
                .Select(t => t.Trim().ToLower())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var term in uniqueTerms)
            {
                TermDf.TryAdd(term, 0);
                TermDf[term]++;
            }
        }

        // === DF для лемм ===
        var lemmaFiles = Directory.GetFiles(LemmasPerDocPath, "*_lemmas.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in lemmaFiles)
        {
            // В файле каждая строка начинается с леммы:
            // <lemma> <token1> <token2> ...
            var uniqueLemmas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {
                var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                var lemma = parts[0].ToLower();
                uniqueLemmas.Add(lemma);
            }

            foreach (var lemma in uniqueLemmas)
            {
                LemmaDf.TryAdd(lemma, 0);
                LemmaDf[lemma]++;
            }
        }

        Console.WriteLine($"Обработано документов (по per-doc токенам): {_totalDocs}");
    }

    /// <summary>
    /// Извлекает термины из файла с использованием общего TextProcessor
    /// (та же логика токенизации, что и в заданиях 2/3).
    /// </summary>
    private static List<string> ExtractTermsFromFile(string filePath)
    {
        var text = TextProcessor.ExtractTextFromHtmlFile(filePath);
        // Для TF-IDF оставляем ту же минимальную длину токена, что и была ( > 2 )
        return TextProcessor.Tokenize(text, minTokenLength: 3).ToList();
    }

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