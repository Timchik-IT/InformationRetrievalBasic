using System.Diagnostics;
using Common;
using DEMO.Models;
using Microsoft.AspNetCore.Mvc;

namespace DEMO.Controllers;

public class HomeController(VectorSearchEngine searchEngine)
    : Controller
{
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}