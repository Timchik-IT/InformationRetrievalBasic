using Common;

namespace HW5;

class Program
{
    private static readonly string Hw4BasePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\HW4\lemmas_terms_per_doc"));

    static void Main(string[] args)
    {
        Console.WriteLine("=== Векторный поиск (HW5) ===\n");

        VectorSearchEngine engine;
        try
        {
            engine = new VectorSearchEngine(Hw4BasePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации поискового движка: {ex.Message}");
            return;
        }

        Console.WriteLine($"Загружено векторов документов: {engine.Documents.Count}\n");

        Console.WriteLine("Введите текстовый запрос (для выхода: exit):\n");

        while (true)
        {
            Console.Write("Запрос > ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(query) ||
                query.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            var results = engine.Search(query, topN: 10);

            if (results.Count == 0)
            {
                Console.WriteLine("Ничего не найдено.\n");
                continue;
            }

            Console.WriteLine($"Найдено документов: {results.Count}");
            foreach (var r in results)
            {
                Console.WriteLine($"  {r.DocumentName}  (score = {r.Score:F4})");
            }

            Console.WriteLine();
        }
    }
}
