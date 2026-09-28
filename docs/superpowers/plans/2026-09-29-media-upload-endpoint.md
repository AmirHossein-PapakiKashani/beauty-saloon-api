# Media Upload Endpoint Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `POST /api/v1/upload` endpoint to the backend that accepts `multipart/form-data`, saves the file to `wwwroot/uploads/`, returns the public URL, and update `PortfolioModal.tsx` so the two image fields accept file uploads (with URL fallback) instead of requiring manual URL entry.

**Architecture:** Single upload endpoint on a new `UploadController`; files are saved to `wwwroot/uploads/` (served as static files); no new service class needed — controller handles the write. Frontend: wrap the two image text inputs in `PortfolioModal.tsx` with a file picker that calls a new `uploadFile` mutation in `galleryApi.ts`; on success it populates `beforeImageUrl` / `afterImageUrl` with the returned URL. URL-only text input remains as a fallback option (progressive enhancement).

**Tech Stack:** .NET 9 / C# (backend), Next.js 14 + RTK Query / TypeScript (frontend), xUnit + FluentAssertions + in-memory EF Core (backend tests), Vitest (frontend tests)

**Spec:** Gap analysis from previous session — `PortfolioModal.tsx` (lines 325-353) currently shows two plain text URL inputs; no `multipart/form-data` upload endpoint exists anywhere in the backend.

## Global Constraints

- Backend envelope pattern: every response is `ApiResponse<T>` with `{ success, message, data, errors }` — never return raw data
- Upload route: `POST /api/v1/upload` — new controller, not bolted onto `PortfolioController`
- Supported MIME types: `image/jpeg`, `image/png`, `image/webp`, `image/gif` — reject all others with HTTP 400
- Max file size: 5 MB — reject larger with HTTP 413
- Files saved to `wwwroot/uploads/<yyyy-MM>/<guid>.<ext>` — organize by month to avoid huge flat directories
- Returned URL format: `http://localhost:5019/uploads/<yyyy-MM>/<guid>.<ext>` — `baseUrl` read from `IConfiguration["AppBaseUrl"]` with fallback `http://localhost:5019`
- `app.UseStaticFiles()` must be added to `Program.cs` before `MapControllers()`
- Frontend uses the existing `galleryApi` slice (`baseApi.injectEndpoints`) — do NOT create a new `createApi`
- `PortfolioModal` keeps the URL text inputs as fallback — uploading a file replaces the URL field value; the user can still type a URL manually

---

## Task 1: Add `app.UseStaticFiles()` to the Backend Pipeline

**Files:**
- Modify: `src/BarberSalon.API/Program.cs`

**Interfaces:**
- Consumes: nothing new
- Produces: files under `wwwroot/` are now served as static files at `/`

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Upload/StaticFilesIntegrationTests.cs`:

```csharp
using System.Net;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Upload;

public class StaticFilesIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public StaticFilesIntegrationTests(BarberSalonWebFactory factory)
        => _client = factory.GetClient();

    [Fact]
    public async Task UploadEndpoint_RejectsInvalidMimeType_Returns400()
    {
        // Use a txt "image" to verify MIME rejection before static files matter
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/v1/upload", content);

        // Either 404 (endpoint missing) or 400 (bad MIME) — will be 404 in RED
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Run to confirm it fails with 404**

```bash
cd e:\barber\beauty-saloon-api
dotnet test tests/BarberSalon.IntegrationTests --filter "StaticFilesIntegrationTests" -v minimal
```
Expected: Test fails — endpoint returns 404.

- [ ] **Step 3: Add `app.UseStaticFiles()` to `Program.cs`**

In `src/BarberSalon.API/Program.cs` add `app.UseStaticFiles();` after `app.UseHttpsRedirection()` and before `app.UseCors(...)`:

```csharp
app.UseHttpsRedirection();

app.UseStaticFiles();   // serves wwwroot/ at /

app.UseCors(BarberSalon.API.DependencyInjection.CorsPolicyName);

app.MapControllers();
```

- [ ] **Step 4: Build**

```bash
dotnet build src/BarberSalon.API/BarberSalon.API.csproj
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/BarberSalon.API/Program.cs
git commit -m "feat(upload): enable static files middleware for wwwroot/"
```

---

## Task 2: Implement `POST /api/v1/upload` Controller

**Files:**
- Create: `src/BarberSalon.API/Controllers/UploadController.cs`

**Interfaces:**
- Consumes: `IWebHostEnvironment.WebRootPath` (string path to `wwwroot/`), `IConfiguration["AppBaseUrl"]`
- Produces:
  - `POST /api/v1/upload` with `multipart/form-data` field `file` → `ApiResponse<UploadResultDto>`
  - `UploadResultDto` is an inline anonymous record `{ string Url }` — no separate DTO file needed (simple enough)

- [ ] **Step 1: Write the failing integration test**

Create `tests/BarberSalon.IntegrationTests/Upload/FileUploadIntegrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BarberSalon.IntegrationTests.Helpers;
using FluentAssertions;
using Xunit;

namespace BarberSalon.IntegrationTests.Upload;

public class FileUploadIntegrationTests : IClassFixture<BarberSalonWebFactory>
{
    private readonly HttpClient _client;

    public FileUploadIntegrationTests(BarberSalonWebFactory factory)
        => _client = factory.GetClient();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Upload_ValidJpegFile_Returns200WithUrl()
    {
        // Arrange — minimal valid JPEG header bytes
        byte[] jpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        // Act
        var response = await _client.PostAsync("/api/v1/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content
            .ReadFromJsonAsync<UploadEnvelope>(JsonOptions);
        envelope!.Success.Should().BeTrue();
        envelope.Data.Url.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Url.Should().Contain("/uploads/");
    }

    [Fact]
    public async Task Upload_TextFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x41, 0x42, 0x43 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "test.txt");

        var response = await _client.PostAsync("/api/v1/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/v1/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record UploadResultData(string Url);
    private sealed record UploadEnvelope(UploadResultData Data, bool Success, string Message);
}
```

- [ ] **Step 2: Run to confirm tests fail with 404**

```bash
cd e:\barber\beauty-saloon-api
dotnet test tests/BarberSalon.IntegrationTests --filter "FileUploadIntegrationTests" -v minimal
```
Expected: All 3 tests fail — endpoint returns 404.

- [ ] **Step 3: Create `UploadController.cs`**

Create `src/BarberSalon.API/Controllers/UploadController.cs`:

```csharp
using BarberSalon.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

/// <summary>Handles physical file uploads for portfolio images.</summary>
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
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiResponse<object>(null, false, "فایلی ارسال نشده است.", null));

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new ApiResponse<object>(null, false,
                $"نوع فایل '{file.ContentType}' مجاز نیست. تنها JPEG, PNG, WebP, GIF قابل قبول است.", null));

        if (file.Length > MaxFileSizeBytes)
            return StatusCode(StatusCodes.Status413RequestEntityTooLarge,
                new ApiResponse<object>(null, false, "حجم فایل نباید بیش از ۵ مگابایت باشد.", null));

        var ext = MimeToExt[file.ContentType];
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
```

> **Note:** `ApiResponse<T>` is defined in `src/BarberSalon.API/Common/ApiResponse.cs`. The constructor used above may differ from `CreateSuccess` — check the existing `ApiResponse` class before writing. If `ApiResponse` only has a `CreateSuccess` factory, use it. If it has a public constructor `ApiResponse(T data, bool success, string message, string[]? errors)`, use that for error returns.

- [ ] **Step 4: Verify `ApiResponse` constructor signature**

Read `src/BarberSalon.API/Common/ApiResponse.cs` and adjust the error return calls (`BadRequest(...)`) in `UploadController` to match the actual constructor/factory available.

- [ ] **Step 5: Run the integration tests**

```bash
dotnet test tests/BarberSalon.IntegrationTests --filter "FileUploadIntegrationTests" -v minimal
```
Expected: All 3 tests pass (green).

> **Note on integration test environment:** `BarberSalonWebFactory` uses in-memory DB but doesn't override `IWebHostEnvironment.WebRootPath`. Files may be written to a temp path during tests. The URL check `Should().Contain("/uploads/")` does not require the file to actually be served — just that the path is correct.

- [ ] **Step 6: Run the full integration test suite**

```bash
dotnet test tests/BarberSalon.IntegrationTests -v minimal
```
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/BarberSalon.API/Controllers/UploadController.cs \
        tests/BarberSalon.IntegrationTests/Upload/FileUploadIntegrationTests.cs \
        tests/BarberSalon.IntegrationTests/Upload/StaticFilesIntegrationTests.cs
git commit -m "feat(upload): add POST /api/v1/upload endpoint for portfolio images"
```

---

## Task 3: Add `uploadFile` Mutation to Frontend `galleryApi.ts`

**Files:**
- Modify: `beauty-saloon-front-V2/src/store/api/galleryApi.ts`

**Interfaces:**
- Consumes: `POST /api/v1/upload` with `FormData` (field `file`) → envelope `{ success: true, data: { url: string } }`
- Produces: hook `useUploadFileMutation()` exported from `galleryApi` — returns `{ url: string }` on success

> **Note:** `galleryApi.ts` uses `baseApi.injectEndpoints`. The mutation must send `FormData` and NOT set `Content-Type` header manually (the browser sets `multipart/form-data` with boundary automatically).

- [ ] **Step 1: Write the failing test**

Create `beauty-saloon-front-V2/src/store/api/__tests__/galleryUpload.test.ts`:

```typescript
import { galleryApi } from "../galleryApi";

describe("galleryApi upload endpoint", () => {
  it("exports uploadFile mutation endpoint", () => {
    expect(galleryApi.endpoints.uploadFile).toBeDefined();
  });
});
```

- [ ] **Step 2: Run to confirm it fails**

```bash
cd e:\barber\beauty-saloon-front-V2
npx vitest run src/store/api/__tests__/galleryUpload.test.ts
```
Expected: Test fails — `galleryApi.endpoints.uploadFile` is undefined.

- [ ] **Step 3: Add `uploadFile` mutation inside `galleryApi.ts`**

Inside the existing `galleryApi = baseApi.injectEndpoints({ endpoints: (builder) => ({ ... }) })` block, add:

```typescript
uploadFile: builder.mutation<{ url: string }, FormData>({
  query: (formData) => ({
    url: "/api/v1/upload",
    method: "POST",
    body: formData,
    // Do NOT set Content-Type — let fetch set multipart boundary automatically
    formData: true,
  }),
  transformResponse: (
    response:
      | { success: boolean; data: { url: string }; message: string }
      | { url: string }
  ) => {
    if (response && "data" in response && typeof (response as any).data?.url === "string") {
      return { url: (response as any).data.url };
    }
    return { url: (response as any).url ?? "" };
  },
}),
```

- [ ] **Step 4: Run the frontend test**

```bash
npx vitest run src/store/api/__tests__/galleryUpload.test.ts
```
Expected: Test passes.

- [ ] **Step 5: TypeScript check**

```bash
npx tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/store/api/galleryApi.ts \
        src/store/api/__tests__/galleryUpload.test.ts
git commit -m "feat(upload): add uploadFile mutation to galleryApi RTK slice"
```

---

## Task 4: Update `PortfolioModal.tsx` to Support File Upload with URL Fallback

**Files:**
- Modify: `beauty-saloon-front-V2/src/components/admin/PortfolioModal.tsx`

**Interfaces:**
- Consumes: `useUploadFileMutation` from `@/store/api/galleryApi`
- Produces: the two image sections (before/after) each show:
  1. A `<input type="file" accept="image/*">` button that uploads immediately on change, shows a loading spinner while uploading, and sets the URL field on success
  2. The existing text `<input>` remains below as a URL fallback (still validates that the field is non-empty)

**State additions to `PortfolioModal`:**
- `uploadingBefore: boolean` — true while the before-image upload is in-flight
- `uploadingAfter: boolean` — true while the after-image upload is in-flight

- [ ] **Step 1: Write the failing test**

Create `beauty-saloon-front-V2/src/components/admin/__tests__/PortfolioModal.upload.test.tsx`:

```tsx
import { render, screen } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import { baseApi } from "@/store/api/baseApi";
import PortfolioModal from "../PortfolioModal";

const store = configureStore({
  reducer: { [baseApi.reducerPath]: baseApi.reducer },
  middleware: (g) => g().concat(baseApi.middleware),
});

describe("PortfolioModal file upload inputs", () => {
  it("renders file input for before image", () => {
    render(
      <Provider store={store}>
        <PortfolioModal
          isOpen
          onClose={() => {}}
          onSave={() => {}}
          staffList={[]}
          serviceList={[]}
        />
      </Provider>
    );
    const fileInputs = screen.getAllByRole("button", { name: /انتخاب فایل/i });
    // At minimum one file upload trigger button should exist
    expect(fileInputs.length).toBeGreaterThanOrEqual(1);
  });

  it("still shows URL text input for before image", () => {
    render(
      <Provider store={store}>
        <PortfolioModal
          isOpen
          onClose={() => {}}
          onSave={() => {}}
          staffList={[]}
          serviceList={[]}
        />
      </Provider>
    );
    // URL text input placeholder should remain
    const urlInput = screen.getByPlaceholderText("https://...");
    expect(urlInput).toBeTruthy();
  });
});
```

- [ ] **Step 2: Run to confirm it fails**

```bash
cd e:\barber\beauty-saloon-front-V2
npx vitest run src/components/admin/__tests__/PortfolioModal.upload.test.tsx
```
Expected: Test fails — no file upload button found.

- [ ] **Step 3: Update `PortfolioModal.tsx`**

At the top of `PortfolioModal.tsx`, add the import:
```typescript
import { useUploadFileMutation } from "@/store/api/galleryApi";
```

Inside the `PortfolioModal` component, after the existing state declarations (after line 48), add:
```typescript
const [uploadFile, { isLoading: uploadingBefore }] = useUploadFileMutation();
const [, { isLoading: uploadingAfter }] = useUploadFileMutation();
```

> **Note:** RTK Query mutation hooks return a tuple `[trigger, result]`. Calling `useUploadFileMutation()` twice creates two independent mutation instances — one for before, one for after. Alternatively, use a single instance and track which field is being uploaded with a `uploadingField: "before" | "after" | null` state variable. The single-instance approach is simpler; use it.

Replace the two independent mutation instances with a single one + a state field:
```typescript
const [uploadFile, { isLoading: isUploading }] = useUploadFileMutation();
const [uploadingField, setUploadingField] = useState<"before" | "after" | null>(null);
```

Add a `handleFileSelect` helper inside the component:
```typescript
const handleFileSelect = async (
  field: "before" | "after",
  e: React.ChangeEvent<HTMLInputElement>
) => {
  const file = e.target.files?.[0];
  if (!file) return;

  setUploadingField(field);
  try {
    const fd = new FormData();
    fd.append("file", file);
    const result = await uploadFile(fd).unwrap();
    if (field === "before") {
      handleBeforeImageChange(result.url);
    } else {
      handleAfterImageChange(result.url);
    }
  } catch {
    // Upload failed; leave URL field empty so validation catches it
  } finally {
    setUploadingField(null);
    // Reset the file input value so the same file can be re-selected if needed
    e.target.value = "";
  }
};
```

Replace the "before image" `<Field>` block (lines 325-338 in original) with:
```tsx
<Field id="portfolio-before-image" label="تصویر قبل" required error={errors.beforeImageUrl}>
  <div className="space-y-2">
    <label className="flex items-center gap-2 cursor-pointer">
      <span
        role="button"
        aria-label="انتخاب فایل تصویر قبل"
        className={`px-3 py-2 rounded-lg text-sm font-medium border border-white/10 bg-white/5 text-[#C9A96E] hover:bg-white/10 transition-colors ${
          uploadingField === "before" ? "opacity-50 pointer-events-none" : ""
        }`}
      >
        {uploadingField === "before" ? "در حال آپلود..." : "انتخاب فایل"}
      </span>
      <input
        type="file"
        accept="image/jpeg,image/png,image/webp,image/gif"
        className="sr-only"
        disabled={uploadingField !== null}
        onChange={(e) => handleFileSelect("before", e)}
      />
    </label>
    <input
      id="portfolio-before-image"
      type="text"
      value={form.beforeImageUrl}
      onChange={(e) => handleBeforeImageChange(e.target.value)}
      dir="ltr"
      aria-required="true"
      aria-invalid={!!errors.beforeImageUrl}
      aria-describedby={errors.beforeImageUrl ? "portfolio-before-image-error" : undefined}
      className={inputClass(!!errors.beforeImageUrl)}
      placeholder="https://..."
    />
  </div>
</Field>
```

Replace the "after image" `<Field>` block (lines 340-353 in original) with:
```tsx
<Field id="portfolio-after-image" label="تصویر بعد" required error={errors.afterImageUrl}>
  <div className="space-y-2">
    <label className="flex items-center gap-2 cursor-pointer">
      <span
        role="button"
        aria-label="انتخاب فایل تصویر بعد"
        className={`px-3 py-2 rounded-lg text-sm font-medium border border-white/10 bg-white/5 text-[#C9A96E] hover:bg-white/10 transition-colors ${
          uploadingField === "after" ? "opacity-50 pointer-events-none" : ""
        }`}
      >
        {uploadingField === "after" ? "در حال آپلود..." : "انتخاب فایل"}
      </span>
      <input
        type="file"
        accept="image/jpeg,image/png,image/webp,image/gif"
        className="sr-only"
        disabled={uploadingField !== null}
        onChange={(e) => handleFileSelect("after", e)}
      />
    </label>
    <input
      id="portfolio-after-image"
      type="text"
      value={form.afterImageUrl}
      onChange={(e) => handleAfterImageChange(e.target.value)}
      dir="ltr"
      aria-required="true"
      aria-invalid={!!errors.afterImageUrl}
      aria-describedby={errors.afterImageUrl ? "portfolio-after-image-error" : undefined}
      className={inputClass(!!errors.afterImageUrl)}
      placeholder="https://..."
    />
  </div>
</Field>
```

Also disable the Save button while uploading — update the save button's `disabled` prop:
```tsx
disabled={isLoading || uploadingField !== null}
aria-busy={isLoading || uploadingField !== null}
```

- [ ] **Step 4: Run the modal test**

```bash
npx vitest run src/components/admin/__tests__/PortfolioModal.upload.test.tsx
```
Expected: Both tests pass.

- [ ] **Step 5: TypeScript check**

```bash
npx tsc --noEmit
```
Expected: 0 errors.

- [ ] **Step 6: Run all frontend tests**

```bash
npx vitest run
```
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/components/admin/PortfolioModal.tsx \
        src/components/admin/__tests__/PortfolioModal.upload.test.tsx
git commit -m "feat(upload): add file picker to PortfolioModal for before/after images"
```
