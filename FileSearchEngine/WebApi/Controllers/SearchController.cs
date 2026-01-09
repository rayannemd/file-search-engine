using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileSearchEngine.Services.Interfaces;
using FileSearchEngine.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.WebApi.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;
        
        public SearchController(
            ISearchService searchService,
            ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResultModel>>> Search([FromQuery] string q, [FromQuery] int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search query cannot be empty");
            }
            
            _logger.LogInformation("Search request received: {Query}", q);
            
            try
            {
                var results = await _searchService.SearchAsync(q, maxResults);
                
                var resultModels = results.Select(r => new SearchResultModel
                {
                    DocumentId = r.DocumentId,
                    FileName = r.FileName,
                    FilePath = r.FilePath,
                    Score = r.Score,
                    Snippet = r.Snippet
                });
                
                return Ok(resultModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search: {Query}", q);
                return StatusCode(500, "An error occurred while processing your search");
            }
        }
    }
}
