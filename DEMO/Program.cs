using Common;

var builder = WebApplication.CreateBuilder(args);

// Добавляем стандартные MVC-контроллеры и представления.
builder.Services.AddControllersWithViews();

// Регистрируем векторный поисковый движок как singleton:
// он один раз загружает TF-IDF вектора из HW4 и переиспользуется
// всеми HTTP-запросами во время работы демо.
builder.Services.AddSingleton(sp =>
{
    var hw4BasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\HW4\lemmas_terms_per_doc"));

    return new VectorSearchEngine(hw4BasePath);
});

var app = builder.Build();

// Конвейер обработки HTTP-запросов.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();