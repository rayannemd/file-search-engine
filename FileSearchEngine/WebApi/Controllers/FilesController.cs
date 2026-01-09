using System;
using System.Threading.Tasks;
using FileSearchEngine.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.WebApi.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<FilesController> _logger;
        
        public FilesController(
            IDocumentService documentService,
            ILogger<FilesController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }
        
        [HttpPost("upload")]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was provided");
            }
            
            _logger.LogInformation("File upload request received: {FileName}, {Size} bytes", file.FileName, file.Length);
            
            try
            {
                using var stream = file.OpenReadStream();
                var result = await _documentService.UploadAsync(stream, file.FileName);
                
                if (result.IndexedSuccessfully)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                return StatusCode(500, "An error occurred while processing your file");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            try
            {
                _logger.LogInformation("File download request received for document: {DocumentId}", id);
                
                var document = await _documentService.GetByIdAsync(id);
                var fileStream = await _documentService.DownloadAsync(id);
                
                var contentType = GetContentType(document.FileExtension);
                
                return File(fileStream, contentType, document.FileName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Document not found: {DocumentId}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {DocumentId}", id);
                return StatusCode(500, $"Error retrieving file: {ex.Message}");
            }
        }
        
        private string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}
