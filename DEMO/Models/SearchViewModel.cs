namespace DEMO.Models;

/// <summary>
/// ViewModel для главной страницы поиска:
/// содержит введённый пользователем запрос и список найденных документов.
/// </summary>
public class SearchViewModel
{
    /// <summary>
    /// Строка поискового запроса, введённая пользователем.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Ранжированный список результатов векторного поиска.
    /// </summary>
    public List<SearchResultViewModel> Results { get; set; } = [];
}

/// <summary>
/// Отображаемая информация об одном документе в выдаче.
/// </summary>
public class SearchResultViewModel
{
    /// <summary>
    /// Имя документа (например, "10.txt").
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Оценка релевантности (косинусное сходство между запросом и документом).
    /// </summary>
    public double Score { get; set; }
}

