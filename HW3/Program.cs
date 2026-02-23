using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace HW3;

class Program
{
    // Директория с предрасчитанными токенами по документам из HW2
    private static readonly string TokensPerDocPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HW2\tokens_per_doc"));

    private static readonly string OutputDir = Directory.GetCurrentDirectory();
    private static readonly string IndexFilePath = Path.Combine(OutputDir, "inverted_index.txt");

    // Инвертированный индекс: термин -> список ID документов
    private static readonly Dictionary<string, HashSet<int>> InvertedIndex = new(StringComparer.OrdinalIgnoreCase);

    // Маппинг ID документа -> имя файла
    private static readonly Dictionary<int, string> DocIdToFileName = new();

    static void Main(string[] args)
    {
        Console.WriteLine("=== Инвертированный индекс и Булев поиск ===\n");

        if (!Directory.Exists(TokensPerDocPath))
        {
            Console.WriteLine($"Ошибка: Папка с токенами '{TokensPerDocPath}' не найдена. Сначала запустите HW2.");
            return;
        }

        // 1. Построение индекса
        BuildIndex();

        // 2. Сохранение индекса в файл
        SaveIndexToFile();

        // 3. Интерактивный поиск
        RunSearchLoop();
    }

    /// <summary>
    /// Построение инвертированного индекса по всем файлам токенов,
    /// предварительно рассчитанным в HW2.
    /// </summary>
    private static void BuildIndex()
    {
        var files = Directory.GetFiles(TokensPerDocPath, "*_tokens.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("Файлы не найдены.");
            return;
        }

        Console.WriteLine($"Обработка {files.Count} файлов токенов...");

        var docId = 0;
        foreach (var file in files)
        {
            try
            {
                // Читаем токены для документа (по одному в строке)
                var tokens = File.ReadAllLines(file, Encoding.UTF8)
                    .Where(t => !string.IsNullOrWhiteSpace(t));

                // Добавляем токены в индекс
                foreach (var token in tokens)
                {
                    if (!InvertedIndex.ContainsKey(token))
                        InvertedIndex[token] = [];

                    InvertedIndex[token].Add(docId);
                }

                // В качестве имени документа используем имя исходного файла из crawled_pages,
                // например: "4.txt" для "4_tokens.txt".
                var tokensBaseName = Path.GetFileNameWithoutExtension(file); // "4_tokens"
                const string suffix = "_tokens";
                string originalName;

                if (tokensBaseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    originalName = tokensBaseName[..^suffix.Length] + ".txt";
                }
                else
                {
                    // Запасной вариант, если шаблон имени изменится
                    originalName = tokensBaseName + ".txt";
                }

                DocIdToFileName[docId] = originalName;
                docId++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Индекс построен. Уникальных терминов: {InvertedIndex.Count}\n");
    }

    /// <summary>
    /// Сохранение индекса в файл
    /// </summary>
    private static void SaveIndexToFile()
    {
        var lines = InvertedIndex
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value.OrderBy(id => id))}")
            .ToList();

        File.WriteAllLines(IndexFilePath, lines, Encoding.UTF8);
        Console.WriteLine($"Индекс сохранён: {IndexFilePath}\n");
    }

    /// <summary>
    /// Интерактивный цикл поиска
    /// </summary>
    private static void RunSearchLoop()
    {
        Console.WriteLine("Введите запрос (AND, OR, NOT, скобки). Пример: (html AND markup) OR css");
        Console.WriteLine("Для выхода введите: exit\n");

        while (true)
        {
            Console.Write("Запрос > ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(query) || query.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                break;

            try
            {
                var results = EvaluateQuery(query);
                PrintResults(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}\n");
            }
        }
    }

    /// <summary>
    /// Вычисление булева запроса
    /// </summary>
    private static HashSet<int> EvaluateQuery(string query)
    {
        // Нормализация: операторы в верхний регистр
        query = Regex.Replace(query, @"\bAND\b|\bOR\b|\bNOT\b", m => m.Value.ToUpper(), RegexOptions.IgnoreCase);

        // Токенизация запроса
        var tokens = TokenizeQuery(query);

        // Построение AST и вычисление
        var parser = new BooleanQueryParser(tokens, InvertedIndex);
        return parser.Parse();
    }

    /// <summary>
    /// Токенизация запроса (с учётом операторов и скобок)
    /// </summary>
    private static List<string> TokenizeQuery(string query)
    {
        var tokens = new List<string>();
        var regex = new Regex(@"\(|\)|\bAND\b|\bOR\b|\bNOT\b|[a-z]+", RegexOptions.IgnoreCase);

        foreach (Match match in regex.Matches(query))
        {
            tokens.Add(match.Value.Equals("AND", StringComparison.CurrentCultureIgnoreCase) ||
                       match.Value.Equals("OR", StringComparison.CurrentCultureIgnoreCase) ||
                       match.Value.Equals("NOT", StringComparison.CurrentCultureIgnoreCase)
                ? match.Value.ToUpper()
                : match.Value.ToLower());
        }

        return tokens;
    }

    /// <summary>
    /// Вывод результатов поиска
    /// </summary>
    private static void PrintResults(HashSet<int> docIds)
    {
        if (docIds.Count == 0)
        {
            Console.WriteLine("Ничего не найдено.\n");
            return;
        }

        Console.WriteLine($"Найдено документов: {docIds.Count}");
        foreach (var id in docIds.OrderBy(x => x))
        {
            if (DocIdToFileName.TryGetValue(id, out var fileName))
                Console.WriteLine($"  [{id}] {fileName}");
        }

        Console.WriteLine();
    }
}