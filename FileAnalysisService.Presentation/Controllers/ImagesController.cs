using FileAnalysisService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FileAnalysisService.Presentation.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IStorageClient _storageClient;
    private readonly IConfiguration _configuration;

    public ImagesController(IStorageClient storageClient, IConfiguration configuration)
    {
        _storageClient = storageClient;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /api/images/{imageKey}
    /// – отдаёт PNG-стрим из MinIO по ключу imageKey.
    /// – если не найдено – 404.
    /// </summary>
    [HttpGet("{imageKey}")]
    public async Task<IActionResult> GetImage([FromRoute] string imageKey, CancellationToken ct)
    {
        var bucketName = _configuration.GetValue<string>("Minio:BucketName", "analytics-images");
        try
        {
            var ms = await _storageClient.GetAsync(bucketName, imageKey, ct);
            return File(ms, "image/png");
        }
        catch
        {
            return NotFound();
        }
    }
}