using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileSearchEngine.Pages
{
    public class ResultsModel : PageModel
    {
        public string Query { get; private set; }
        
        public void OnGet(string q)
        {
            Query = q;
        }
    }
}
