using BarberSalon.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>Handles physical file uploads for portfolio images and salon media.</summary>
[ApiController]
[Route("api/v1/upload")]
public sealed class UploadController(
    IWebHostEnvironment env,
    IConfiguration configuration,
    ILogger<UploadController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private static readonly Dictionary<string, string> MimeToExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object?>(null, false, "فایلی ارسال نشده است."));
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            return BadRequest(new ApiResponse<object?>(null, false,
                $"نوع فایل '{file.ContentType}' مجاز نیست. تنها JPEG, PNG, WebP, GIF قابل قبول است."));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new ApiResponse<object?>(null, false, "حجم فایل نباید بیش از ۵ مگابایت باشد."));
        }

        var ext = MimeToExt.TryGetValue(file.ContentType, out var matchedExt) ? matchedExt : Path.GetExtension(file.FileName);
        var monthFolder = DateTime.UtcNow.ToString("yyyy-MM");
        var fileName = $"{Guid.NewGuid()}{ext}";

        var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads", monthFolder);
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var baseUrl = configuration["AppBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5019";
        var publicUrl = $"{baseUrl}/uploads/{monthFolder}/{fileName}";

        logger.LogInformation("File uploaded: {FilePath}, public URL: {Url}", filePath, publicUrl);

        return Ok(ApiResponse<object>.CreateSuccess(new { url = publicUrl }, "فایل با موفقیت آپلود شد."));
    }
}
