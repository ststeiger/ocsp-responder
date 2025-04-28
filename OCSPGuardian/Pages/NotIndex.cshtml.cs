using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OCSPGuardian.Pages
{
    public class NotIndexModel : PageModel
    {
        private readonly Microsoft.Extensions.Logging.ILogger<IndexModel> m_logger;

        public NotIndexModel(Microsoft.Extensions.Logging.ILogger<IndexModel> logger)
        {
            this.m_logger = logger;
        }

        public void OnGet()
        {
            System.Console.WriteLine(this.m_logger);
        }
    }
}
