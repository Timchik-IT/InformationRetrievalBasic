using Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Регистрируем векторный поисковый движок как singleton
builder.Services.AddSingleton(sp =>
{
    var hw4BasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\HW4\lemmas_terms_per_doc"));

    return new VectorSearchEngine(hw4BasePath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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