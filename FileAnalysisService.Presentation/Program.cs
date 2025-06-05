using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Infrastructure.Persistence;
using FileAnalysisService.Infrastructure.Repositories;
using FileAnalysisService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. Настраиваем DbContext для PostgreSQL
builder.Services.AddDbContext<FileAnalyticsDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("Postgres")
               ?? throw new InvalidOperationException("ConnectionStrings:Postgres не найден.");
    options.UseNpgsql(conn);
});

// 2. Регистрируем репозиторий
builder.Services.AddScoped<IFileAnalysisRepository, FileAnalysisRepository>();

// 3. Настраиваем gRPC-клиент (IFileStorageClient)
builder.Services.AddSingleton<IFileStorageClient, FileStorageGrpcClient>();

// 4. Конфигурация MinIO
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.AddSingleton<IStorageClient, MinioStorageClient>();

// 5. Настраиваем HTTP-клиент для QuickChart
builder.Services.AddHttpClient<IWordCloudApiClient, WordCloudApiClient>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("WordCloudApi:BaseUrl")
                  ?? throw new InvalidOperationException("WordCloudApi:BaseUrl не найден.");
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// 6. MediatR (Application)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(Assembly.Load("FileAnalysisService.Application"));
});

// 7. Добавляем контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileAnalyticsService API", Version = "v1" });
});

// 8. (Опционально) Kestrel: если нужен какой-либо порт, настроить здесь.

// === Сборка приложения ===
var app = builder.Build();

// Применяем миграции автоматически (если требуется)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileAnalyticsDbContext>();
    db.Database.Migrate();
}

// 9. Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileAnalyticsService API v1");
        c.RoutePrefix = "swagger";
    });
}

// Нет необходимости в HTTPS Redirect (работаем в Docker/HTTP)
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Простое «ping»
app.MapGet("/", () => "FileAnalysisService is running…");

app.Run();