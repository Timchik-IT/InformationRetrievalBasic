using System.Text;

namespace Common;

/// <summary>
/// Результат поиска: имя документа и его score.
/// </summary>
/// <param name="DocumentName">Имя документа (например, "10.txt").</param>
/// <param name="Score">Косинусное сходство (чем больше, тем релевантнее).</param>
public record SearchResult(string DocumentName, double Score);

/// <summary>
/// Векторный поисковый движок на основе TF-IDF, посчитанного в HW4.
/// </summary>
public class VectorSearchEngine
{
    private readonly Dictionary<string, Dictionary<string, double>> _docTermVectors =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Загруженные документы и их TF-IDF веса:
    /// documentName -> (term -> tf-idf(term, document)).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Documents =>
        _docTermVectors.ToDictionary(
            kvp => kvp.Key, IReadOnlyDictionary<string, double> (kvp) => kvp.Value,
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Создаёт движок и загружает TF-IDF вектора документов из директории HW4.
    /// Ожидается, что в директории лежат файлы вида "*_terms.txt" со строками:
    /// "&lt;term&gt; &lt;idf&gt; &lt;tf-idf&gt;".
    /// </summary>
    /// <param name="hw4TermsDirectory">Путь к папке с результатами HW4 (например, "HW4/lemmas_terms_per_doc").</param>
    public VectorSearchEngine(string hw4TermsDirectory)
    {
        if (!Directory.Exists(hw4TermsDirectory))
            throw new DirectoryNotFoundException(
                $"Папка с результатами HW4 не найдена: '{hw4TermsDirectory}'.");

        LoadDocumentVectors(hw4TermsDirectory);

        if (_docTermVectors.Count == 0)
            throw new InvalidOperationException("Не удалось загрузить ни одного вектора документа.");
    }

    /// <summary>
    /// Векторный поиск.
    /// Документы представлены TF-IDF векторами, запрос представляется TF-вектором
    /// (частоты терминов в запросе), после чего считается косинусное сходство.
    /// </summary>
    /// <param name="query">Строка запроса.</param>
    /// <param name="topN">Сколько лучших результатов вернуть (по умолчанию 10).</param>
    public List<SearchResult> Search(string query, int topN = 10)
    {
        var queryTokens = TextProcessor.Tokenize(query, minTokenLength: 3).ToList();
        if (queryTokens.Count == 0)
            return [];

        var termCounts = queryTokens.GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var totalTerms = queryTokens.Count;
        var queryVector = termCounts.ToDictionary(
            kvp => kvp.Key,
            kvp => (double)kvp.Value / totalTerms,
            StringComparer.OrdinalIgnoreCase);

        var results = new List<SearchResult>();

        foreach (var (docName, docVector) in _docTermVectors)
        {
            var score = CosineSimilarity(queryVector, docVector);
            if (score > 0)
                results.Add(new SearchResult(docName, score));
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(topN)
            .ToList();
    }

    /// <summary>
    /// Загружает TF-IDF вектора документов из файлов "*_terms.txt".
    /// Имя документа восстанавливается из имени файла: "10_terms.txt" -> "10.txt".
    /// </summary>
    /// <param name="hw4TermsDirectory">Папка с результатами HW4.</param>
    private void LoadDocumentVectors(string hw4TermsDirectory)
    {
        var termFiles = Directory.GetFiles(hw4TermsDirectory, "*_terms.txt")
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
                _docTermVectors[docName] = vector;
        }
    }

    /// <summary>
    /// Косинусное сходство двух разреженных векторов "term -> weight".
    /// Возвращает число в диапазоне [0..1] (при неотрицательных весах).
    /// </summary>
    /// <param name="v1">Вектор запроса (term -> weight).</param>
    /// <param name="v2">Вектор документа (term -> weight).</param>
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
}

