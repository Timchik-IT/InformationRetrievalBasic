using System.Text;
using Common;
using opennlp.tools.stemmer;

namespace HW2;

class Program
{
    // Пути к данным и результатам
    private static readonly string DataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\HW1\crawled_pages"));
    private static readonly string OutputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
    private static readonly string TokensPath = Path.Combine(OutputDir, "tokens.txt");
    private static readonly string LemmasPath = Path.Combine(OutputDir, "lemmas.txt");
    private static readonly string PerDocTokensDir = Path.Combine(OutputDir, "tokens_per_doc");
    private static readonly string PerDocLemmasDir = Path.Combine(OutputDir, "lemmas_per_doc");

    // Стеммер Портера для приведения слов к основе
    private static readonly PorterStemmer Stemmer = new();

    static void Main(string[] args)
    {
        // Основной пайплайн обработки:
        // 1) считаем токены и леммы для КАЖДОГО файла и сохраняем отдельные файлы;
        // 2) параллельно накапливаем глобальные списки для tokens.txt и lemmas.txt.
        var (tokens, lemmas) = ProcessFilesPerDocument();

        // Сохранение результатов
        SaveResults(tokens, lemmas);

        Console.WriteLine($"Готово! Токенов: {tokens.Count}, Групп (лемм): {lemmas.Count}");
    }

    /// <summary>
    /// Основной метод обработки: читает каждый файл, извлекает текст, токенизирует и
    /// сохраняет токены и леммы ПО ФАЙЛАМ, одновременно накапливая глобальные списки.
    /// </summary>
    /// <returns>Глобальные: набор уникальных токенов и словарь лемм</returns>
    private static (HashSet<string> tokens, Dictionary<string, List<string>> lemmas) ProcessFilesPerDocument()
    {
        Directory.CreateDirectory(PerDocTokensDir);
        Directory.CreateDirectory(PerDocLemmasDir);

        var allTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allLemmas = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var files = Directory.GetFiles(DataPath, "*.txt");

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Извлекаем текст и токенизируем (минимальная длина токена 2, как в HW2)
            var text = TextProcessor.ExtractTextFromHtmlFile(file);
            var docTokens = TextProcessor.Tokenize(text, minTokenLength: 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (docTokens.Count == 0)
                continue;

            // Сохраняем токены текущего документа: по одному в строке
            var perDocTokensPath = Path.Combine(PerDocTokensDir, $"{fileName}_tokens.txt");
            File.WriteAllLines(perDocTokensPath, docTokens.OrderBy(t => t), Encoding.UTF8);

            // Группируем токены по стеммам для текущего документа
            var docLemmas = GroupTokensByStem(docTokens);

            // Сохраняем леммы текущего документа:
            // <лемма> <токен1> <токен2> ... <токенN>
            var perDocLemmaLines = docLemmas
                .Select(g => $"{g.Key} {string.Join(" ", g.Value.OrderBy(t => t))}")
                .OrderBy(l => l);

            var perDocLemmasPath = Path.Combine(PerDocLemmasDir, $"{fileName}_lemmas.txt");
            File.WriteAllLines(perDocLemmasPath, perDocLemmaLines, Encoding.UTF8);

            // Накапливаем глобальные токены и леммы
            allTokens.UnionWith(docTokens);

            foreach (var kvp in docLemmas)
            {
                if (!allLemmas.TryGetValue(kvp.Key, out var list))
                {
                    list = new List<string>();
                    allLemmas[kvp.Key] = list;
                }

                list.AddRange(kvp.Value);
            }
        }

        // Удаляем дубликаты токенов внутри списков лемм и сортируем
        foreach (var key in allLemmas.Keys.ToList())
        {
            allLemmas[key] = allLemmas[key]
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();
        }

        return (allTokens, allLemmas);
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
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && !TextProcessor.StopWords.Contains(g.Key))
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