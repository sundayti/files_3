// File: FileAnalysisService.Presentation/Program.cs

using System;
using Filestoring; // namespace из вашего file_storage.proto (sgenerrirovannyi gRPC-клиент)
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Пространства имён ваших проектов
using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Infrastructure.Persistence;
using FileAnalysisService.Infrastructure.Repositories;
using FileAnalysisService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// 1. Настроить конфигурацию (чтение из appsettings.json / .env)
// ----------------------------------------------------

// Пример appsettings.json (или его эквивалент в Docker-.env):
//
// {
//   "ConnectionStrings": {
//     "Postgres": "Host=fileanalysis-postgres;Port=5432;Database=analyticsdb;Username=postgres;Password=postgres"
//   },
//   "Minio": {
//     "Endpoint": "fileanalysis-minio:9000",
//     "AccessKey": "minio-access-key",
//     "SecretKey": "minio-secret-key",
//     "BucketName": "analytics-bucket",
//     "UseSSL": false
//   },
//   "FileStoring": {
//     // Вот это значение вы заменяете на реальный адрес первого сервиса:
//     "GrpcEndpoint": "http://84.201.169.225:5001"
//   },
//   "QuickChart": {
//     "BaseUrl": "https://quickchart.io",   // Endpoint QuickChart
//     "Width": 800,
//     "Height": 600,
//     "BackgroundColor": "white"
//   },
//   "Logging": {
//     "LogLevel": {
//       "Default": "Information",
//       "Microsoft.AspNetCore": "Warning"
//     }
//   }
// }

IConfiguration configuration = builder.Configuration;

// ----------------------------------------------------
// 2. Добавить DbContext (EF Core) для FileAnalysisService
// ----------------------------------------------------
builder.Services.AddDbContext<FileAnalysisDbContext>(options =>
{
    // Читаем строку подключения из конфигурации:
    var connStr = configuration.GetConnectionString("Postgres") 
                  ?? configuration["ConnectionStrings:Postgres"];
    options.UseNpgsql(connStr);
});

// ----------------------------------------------------
// 3. Зарегистрировать репозитории/интерфейсы
// ----------------------------------------------------
// Предполагаем, что у вас есть интерфейс IAnalysisResultRepository, 
// и его реализация AnalysisResultRepository
builder.Services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();

// ----------------------------------------------------
// 4. Зарегистрировать Minio-клиент
// ----------------------------------------------------
// Допустим, у вас есть интерфейс IMinioStorageClient и класс MinioStorageClient
builder.Services.AddSingleton<IMinioStorageClient, MinioStorageClient>();

// ----------------------------------------------------
// 5. Зарегистрировать gRPC-клиент, чтобы ходить в FileStoringService
// ----------------------------------------------------
builder.Services.AddGrpcClient<Filestoring.FileStorage.FileStorageClient>(options =>
{
    // Тут мы берём URL из конфигурации. В appsettings.json должно быть:
    // "FileStoring": { "GrpcEndpoint": "http://84.201.169.225:5001" }
    var grpcEndpoint = configuration["FileStoring:GrpcEndpoint"];
    if (string.IsNullOrWhiteSpace(grpcEndpoint))
    {
        throw new InvalidOperationException(
            "FileStoring:GrpcEndpoint не настроен в конфигурации. " +
            "Добавьте его в appsettings.json или .env."
        );
    }
    options.Address = new Uri(grpcEndpoint);
});

// Оборачиваем сгенерированный gRPC-клиент в наш “удобный” класс FileStorageGrpcClient
builder.Services.AddScoped<FileStorageGrpcClient>();

// ----------------------------------------------------
// 6. Зарегистрировать сервис генерации облака слов (QuickChartWordCloudService)
// ----------------------------------------------------
// Сначала зарегистрируем named HttpClient, чтобы задать таймаут и т. п.
builder.Services.AddHttpClient("QuickChartClient", client =>
{
    // Можно дополнительно настроить базовый адрес, таймаут и пр.
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Затем регистрируем наш IWordCloudService, передавая в конструктор тот самый HttpClient
builder.Services.AddScoped<IWordCloudService>(sp =>
{
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient  = httpFactory.CreateClient("QuickChartClient");
    var logger      = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QuickChartWordCloudService>>();
    var config      = sp.GetRequiredService<IConfiguration>();
    return new QuickChartWordCloudService(httpClient, logger, config);
});

// ----------------------------------------------------
// 7. Зарегистрировать MediatR (Application-слой)
// ----------------------------------------------------
// Предполагая, что все ваши IRequest/Handlers лежат в сборке FileAnalysisService.Application
builder.Services.AddMediatR(typeof(AnalyzeFileCommand).Assembly);

// ----------------------------------------------------
// 8. Добавить контроллеры и Swagger
// ----------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
});

// ----------------------------------------------------
// 9. Настроить Kestrel (чтобы отключить HTTPS, если нужно, и включить HTTP/2 для gRPC)
// ----------------------------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    // Позволяем слушать и HTTP/1.1, и HTTP/2 (gRPC без TLS будет работать по HTTP/2 plaintext).
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        // Мы не вызываем UseHttps(), потому что внутри Docker обычно делаем plaintext gRPC.
    });
});

// ----------------------------------------------------
// Построить и запустить
// ----------------------------------------------------
var app = builder.Build();

// Если вы хотите пользоваться Swagger UI:
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileAnalyticsService API v1");
        c.RoutePrefix = "swagger";
    });
}

// Нет необходимости в HTTPS Redirect, потому что мы работаем на HTTP (Docker)
// app.UseHttpsRedirection();

app.MapControllers();

// Для проверки работоспособности:
app.MapGet("/", () => "FileAnalysisService is running…");

app.Run();