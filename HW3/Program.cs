using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace HW3;

class Program
{
    private static readonly string DataPath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HW1\crawled_pages"));

    private static readonly string OutputDir = Directory.GetCurrentDirectory();
    private static readonly string IndexFilePath = Path.Combine(OutputDir, "inverted_index.txt");

    // Стоп-слова для фильтрации
    private static readonly HashSet<string> StopWords =
    [
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "up", "about", "into", "through", "during", "before", "after", "above", "below", "between", "both",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "can", "may", "might", "must", "i", "you", "he", "she", "it",
        "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "its", "our", "their",
        "this", "that", "these", "those", "am", "as", "if", "when", "where", "why", "how", "all", "each",
        "every", "few", "more", "most", "other", "some", "such", "no", "nor", "not", "only", "own", "same",
        "so", "than", "too", "very", "just", "also", "now", "here", "there", "then", "once"
    ];

    // Инвертированный индекс: термин -> список ID документов
    private static readonly Dictionary<string, HashSet<int>> InvertedIndex = new(StringComparer.OrdinalIgnoreCase);

    // Маппинг ID документа -> имя файла
    private static readonly Dictionary<int, string> DocIdToFileName = new();

    static void Main(string[] args)
    {
        Console.WriteLine("=== Инвертированный индекс и Булев поиск ===\n");

        if (!Directory.Exists(DataPath))
        {
            Console.WriteLine($"Ошибка: Папка '{DataPath}' не найдена.");
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
    /// Построение инвертированного индекса по всем файлам
    /// </summary>
    private static void BuildIndex()
    {
        var files = Directory.GetFiles(DataPath, "*.txt").ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("Файлы не найдены.");
            return;
        }

        Console.WriteLine($"Обработка {files.Count} файлов...");

        int docId = 0;
        foreach (var file in files)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(File.ReadAllText(file, Encoding.UTF8));

                // Извлекаем текст
                var text = doc.DocumentNode.InnerText;

                // Токенизация
                var tokens = Tokenize(text);

                // Добавляем токены в индекс
                foreach (var token in tokens)
                {
                    if (!InvertedIndex.ContainsKey(token))
                        InvertedIndex[token] = [];

                    InvertedIndex[token].Add(docId);
                }

                DocIdToFileName[docId] = Path.GetFileName(file);
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
    /// Токенизация текста: извлечение слов, фильтрация
    /// </summary>
    private static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(text.ToLower(), @"[a-z]+")
            .Select(m => m.Value)
            .Where(t => t.Length > 2 && !StopWords.Contains(t));
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

/// <summary>
/// Парсер булевых запросов с поддержкой скобок и приоритетов
/// </summary>
public class BooleanQueryParser(List<string> tokens, Dictionary<string, HashSet<int>> index)
{
    private int _position;

    /// <summary>
    /// Парсинг и вычисление запроса
    /// </summary>
    public HashSet<int> Parse()
    {
        if (tokens.Count == 0)
            return [];

        var result = ParseOrExpression();

        if (_position < tokens.Count)
            throw new Exception($"Неожиданный токен: {tokens[_position]}");

        return result;
    }

    /// <summary>
    /// OR имеет наименьший приоритет
    /// </summary>
    private HashSet<int> ParseOrExpression()
    {
        var left = ParseAndExpression();

        while (_position < tokens.Count && tokens[_position] == "OR")
        {
            _position++; // пропускаем OR
            var right = ParseAndExpression();
            left = Union(left, right);
        }

        return left;
    }

    /// <summary>
    /// AND имеет средний приоритет
    /// </summary>
    private HashSet<int> ParseAndExpression()
    {
        var left = ParseNotExpression();

        while (_position < tokens.Count && tokens[_position] == "AND")
        {
            _position++; // пропускаем AND
            var right = ParseNotExpression();
            left = Intersect(left, right);
        }

        return left;
    }

    /// <summary>
    /// NOT имеет наивысший приоритет
    /// </summary>
    private HashSet<int> ParseNotExpression()
    {
        if (_position < tokens.Count && tokens[_position] == "NOT")
        {
            _position++; // пропускаем NOT
            var operand = ParsePrimary();
            return Complement(operand);
        }

        return ParsePrimary();
    }

    /// <summary>
    /// Терм или выражение в скобках
    /// </summary>
    private HashSet<int> ParsePrimary()
    {
        if (_position >= tokens.Count)
            throw new Exception("Неожиданный конец запроса");

        var token = tokens[_position];

        if (token == "(")
        {
            _position++; // пропускаем (
            var result = ParseOrExpression();

            if (_position >= tokens.Count || tokens[_position] != ")")
                throw new Exception("Ожидалась закрывающая скобка )");

            _position++; // пропускаем )
            return result;
        }

        if (token == "AND" || token == "OR" || token == "NOT" || token == ")")
            throw new Exception($"Неожиданный оператор: {token}");

        // Термин
        _position++;
        return GetDocsForTerm(token);
    }

    /// <summary>
    /// Получение списка документов для термина
    /// </summary>
    private HashSet<int> GetDocsForTerm(string term)
    {
        if (index.TryGetValue(term, out var docs))
            return [..docs];

        return [];
    }

    /// <summary>
    /// Объединение множеств (OR)
    /// </summary>
    private static HashSet<int> Union(HashSet<int> a, HashSet<int> b)
    {
        var result = new HashSet<int>(a);
        result.UnionWith(b);
        return result;
    }

    /// <summary>
    /// Пересечение множеств (AND)
    /// </summary>
    private static HashSet<int> Intersect(HashSet<int> a, HashSet<int> b)
    {
        var result = new HashSet<int>(a);
        result.IntersectWith(b);
        return result;
    }

    /// <summary>
    /// Дополнение множества (NOT)
    /// </summary>
    private HashSet<int> Complement(HashSet<int> a)
    {
        var allDocs = new HashSet<int>(index.Values.SelectMany(v => v).Distinct());
        allDocs.ExceptWith(a);
        return allDocs;
    }
}