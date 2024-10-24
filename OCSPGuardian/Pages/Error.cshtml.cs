using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace OCSPGuardian.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly Microsoft.Extensions.Logging.ILogger<ErrorModel> m_logger;

        public ErrorModel(Microsoft.Extensions.Logging.ILogger<ErrorModel> logger)
        {
            this.m_logger = logger;
        }

        public void OnGet()
        {
            System.Console.WriteLine(this.m_logger);
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }

}
