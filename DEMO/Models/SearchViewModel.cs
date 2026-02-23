namespace DEMO.Models;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;

    public List<SearchResultViewModel> Results { get; set; } = [];
}

public class SearchResultViewModel
{
    public string DocumentName { get; set; } = string.Empty;

    public double Score { get; set; }
}

