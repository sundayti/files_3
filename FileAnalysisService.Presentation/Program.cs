// File: FileAnalysisService.Presentation/Program.cs

using System;                                     // ← для Uri
using FileAnalysisService.Application.Commands;
using FileAnalysisService.Application.Interfaces;
using FileAnalysisService.Application.Queries;
using FileAnalysisService.Domain.Interfaces;
using FileAnalysisService.Infrastructure.Persistence;
using FileAnalysisService.Infrastructure.Repositories;
using FileAnalysisService.Infrastructure.Services;
using Filestoring;                                // ← пространство имён сгенерированного gRPC-клиента
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ↓↓↓ Обязательно! Читаем GrpcEndpoint из appsettings.json ↓↓↓
var grpcEndpoint = configuration.GetValue<string>("FileStoring:GrpcEndpoint");

// 1) Конфигурация (appsettings.json + ENV)
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

// 4) MinIO (наследуем из Infrastructure)
builder.Services.Configure<MinioSettings>(
    configuration.GetSection("Minio")
);
builder.Services.AddSingleton<IStorageClient, MinioStorageClient>();

// 5) gRPC-клиент к FileStoringService: 
//    — AddGrpcClient «под капотом» создаст GrpcChannel.ForAddress(grpcEndpoint) 
//    — и зарегистрирует FileStorage.FileStorageClient
builder.Services.AddGrpcClient<FileStorage.FileStorageClient>(o =>
{
    o.Address = new Uri(grpcEndpoint);
});
builder.Services.AddScoped<IFileStorageClient, FileStorageGrpcClient>();

// 6) HTTP-клиент для внешнего WordCloud API
builder.Services.Configure<WordCloudApiClientSettings>(
    configuration.GetSection("WordCloudApi")
);
builder.Services.AddHttpClient<IWordCloudApiClient, WordCloudApiClient>();

// 7) MediatR (регистрируем Application-ассембли)
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

// 9) Kestrel: слушаем HTTP/1.1 и HTTP/2 на 5002 
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

var app = builder.Build();

// 10) Автоматические миграции при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileAnalyticsDbContext>();
    db.Database.Migrate();
}

// 11) Pipeline middleware
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

// Если не нужен HTTPS (внутри Docker обычно HTTP/2 plaintext достаточно)
// app.UseHttpsRedirection();
// app.UseRouting();
// app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "FileAnalyticsService is running.");

app.Run();