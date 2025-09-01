
namespace TestWebSocketProxy.Pages
{

    using Microsoft.Extensions.Logging;


    public class IndexModel(Microsoft.Extensions.Logging.ILogger<IndexModel> logger)
        : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        
        public void OnGet()
        {
            logger.LogInformation("Page accessed");
        }

    }


}
