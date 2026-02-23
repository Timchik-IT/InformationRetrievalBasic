using System.Text;

namespace Common;

public record SearchResult(string DocumentName, double Score);

/// <summary>
/// Векторный поисковый движок на основе TF-IDF, посчитанного в HW4.
/// </summary>
public class VectorSearchEngine
{
    private readonly Dictionary<string, Dictionary<string, double>> _docTermVectors =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Documents =>
        _docTermVectors.ToDictionary(
            kvp => kvp.Key, IReadOnlyDictionary<string, double> (kvp) => kvp.Value,
            StringComparer.OrdinalIgnoreCase);

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
    /// Векторный поиск: строим вектор запроса и считаем косинусное сходство с каждым документом.
    /// </summary>
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

