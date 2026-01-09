using System.ComponentModel.DataAnnotations;

namespace FileSearchEngine.WebApi.Models
{
    public class SearchRequestModel
    {
        [Required]
        public string Query { get; set; }
        
        public int MaxResults { get; set; } = 10;
    }
}
