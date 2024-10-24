using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OCSPGuardian.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly Microsoft.Extensions.Logging.ILogger<PrivacyModel> m_logger;

        public PrivacyModel(Microsoft.Extensions.Logging.ILogger<PrivacyModel> logger)
        {
            this.m_logger = logger;
        }

        public void OnGet()
        {
            System.Console.WriteLine(this.m_logger);
        }
    }

}
