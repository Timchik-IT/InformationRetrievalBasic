using System.Diagnostics;
using Common;
using DEMO.Models;
using Microsoft.AspNetCore.Mvc;

namespace DEMO.Controllers;

/// <summary>
/// Веб-контроллер главной страницы демо-поисковика.
/// Принимает текстовый запрос, вызывает векторный движок и
/// отдаёт вьюхе ранжированный список документов.
/// </summary>
/// <param name="searchEngine" cref="VectorSearchEngine">Векторный поисковый движок.</param>
public class HomeController(VectorSearchEngine searchEngine)
    : Controller
{
    /// <summary>
    /// Главная страница: отображает форму поиска и, при наличии запроса,
    /// показывает топ-10 документов по косинусному сходству.
    /// </summary>
    /// <param name="query">Текст поискового запроса из строки браузера.</param>
    [HttpGet]
    public IActionResult Index(string? query)
    {
        var model = new SearchViewModel { Query = query ?? string.Empty };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var results = searchEngine.Search(query, topN: 10);
            model.Results = results
                .Select(r => new SearchResultViewModel
                {
                    DocumentName = r.DocumentName,
                    Score = r.Score
                })
                .ToList();
        }

        return View(model);
    }

    /// <summary>
    /// Статическая страница с политикой конфиденциальности
    /// (оставлена от стандартного шаблона ASP.NET Core).
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Страница с информацией об ошибке запроса.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}