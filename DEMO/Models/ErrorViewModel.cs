namespace DEMO.Models;

/// <summary>
/// ViewModel для страницы ошибки (стандартный шаблон ASP.NET Core MVC).
/// Позволяет показать пользователю идентификатор запроса, чтобы проще было найти
/// подробности в логах/трейсинге.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Идентификатор запроса, который можно использовать для диагностики.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Нужно ли отображать RequestId на странице (если он задан).
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}