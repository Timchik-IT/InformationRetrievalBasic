namespace Common;

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

        if (token is "AND" or "OR" or "NOT" or ")")
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