using FileImportAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileImportAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportDataController : ControllerBase
    {
        private readonly IImportDataService _importDataService;
        private readonly ILogger<ImportDataController> _logger;

        public ImportDataController(IImportDataService importDataService, ILogger<ImportDataController> logger)
        {
            _importDataService = importDataService;
            _logger = logger;
        }

        [HttpPost("upload-single")]
        public async Task<IActionResult> UploadSingleFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid file");

            try
            {
                await _importDataService.ProcessFileAsync(file);
                return Ok("File uploaded successfully");
            }
            catch (Exception ex)
            {
                return Conflict($"File processing error: {ex.Message}");
            }
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleFiles([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Please upload one or more files.");

            var results = new List<string>(); // Collect results for each file

            foreach (var file in files)
            {
                try
                {
                    await _importDataService.ProcessFileAsync(file);
                    results.Add($"{file.FileName}: Uploaded successfully.");
                }
                catch (Exception ex)
                {
                    // Log the error and add to results
                    _logger.LogError($"Error processing {file.FileName}: {ex.Message}");
                    results.Add($"{file.FileName}: Error - {ex.Message}");
                }
            }

            // Return all results for the files
            return Ok(string.Join("\n", results));
        }

    }
}
