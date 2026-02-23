using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Common;

/// <summary>
/// Общие методы для извлечения текста из HTML и токенизации.
/// </summary>
public static class TextProcessor
{
    /// <summary>
    /// Набор стоп-слов (союзы, предлоги, местоимения и т.п.).
    /// </summary>
    public static readonly HashSet<string> StopWords =
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

    /// <summary>
    /// Читает HTML-файл и возвращает видимый текст без разметки.
    /// </summary>
    public static string ExtractTextFromHtmlFile(string filePath)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(File.ReadAllText(filePath, Encoding.UTF8));
        return doc.DocumentNode.InnerText;
    }

    /// <summary>
    /// Токенизирует текст: оставляет только слова из латинских букв,
    /// приводит к нижнему регистру, отбрасывает короткие токены и стоп-слова.
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <param name="minTokenLength">Минимальная длина токена (по умолчанию 2).</param>
    public static IEnumerable<string> Tokenize(string text, int minTokenLength = 2)
    {
        return Regex.Matches(text.ToLower(), @"[a-z]+")
            .Select(m => m.Value)
            .Where(t => t.Length >= minTokenLength && !StopWords.Contains(t));
    }
}

