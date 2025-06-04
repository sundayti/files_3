// File: FileAnalysisService.Presentation/Program.cs

using System.Reflection;
using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Application.Queries;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Infrastructure.Persistence;
using FileAnalysisService.Infrastructure.Repositories;
using FileAnalysisService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 1) Конфигурация (appsettings.json + EnvironmentVariables)
builder.Configuration
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddEnvironmentVariables();

// 2) EF Core + PostgreSQL
var connectionString = configuration.GetConnectionString("Postgres");
builder.Services.AddDbContext<FileAnalyticsDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// 3) Репозиторий
builder.Services.AddScoped<IFileAnalysisRepository, FileAnalysisRepository>();

// 4) MinIO (скопированные из первого сервиса)
//    МинюСеттингс и клиента регистрируем из Infrastructure
builder.Services.Configure<MinioSettings>(
    configuration.GetSection("Minio")
);
builder.Services.AddSingleton<IStorageClient, MinioStorageClient>();

// 5) gRPC-клиент к FileStoringService
var grpcEndpoint = configuration.GetValue<string>("FileStoring:GrpcEndpoint");
builder.Services.AddSingleton<IFileStorageClient>(_ =>
    new FileStorageGrpcClient(grpcEndpoint)
);

// 6) HTTP-клиент для WordCloud API
builder.Services.Configure<WordCloudApiClientSettings>(
    configuration.GetSection("WordCloudApi")
);
builder.Services.AddHttpClient<IWordCloudApiClient, WordCloudApiClient>();

// 7) MediatR (Application-ассембли, где лежат команды и запросы)
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(AnalyzeFileCommand).Assembly)
    );


// 8) Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FileAnalyticsService API",
        Version = "v1"
    });
});

// 9) Kestrel: слушаем HTTP/1.1 и HTTP/2 на порту 5002
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

var app = builder.Build();

// 10) Применим миграции автоматически при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileAnalyticsDbContext>();
    db.Database.Migrate();
}

// 11) Конфигурация middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileAnalyticsService API v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection(); // Если нужна HTTPS (в Docker обычно не ставят)
// app.UseRouting();
// app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "FileAnalyticsService is running.");

app.Run();