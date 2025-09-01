
namespace EnvoyPlaneController.Pages
{
    [Microsoft.AspNetCore.Mvc.ResponseCache(
        Duration = 0, 
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None, 
        NoStore = true)
    ]
    [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
    public class ErrorModel(Microsoft.Extensions.Logging.ILogger<ErrorModel> logger)
        : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // private readonly Microsoft.Extensions.Logging.ILogger<ErrorModel> _logger;

        //public ErrorModel(Microsoft.Extensions.Logging.ILogger<ErrorModel> logger)
        //{
        //    _logger = logger;
        //}

        public void OnGet()
        {
            this.RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if (logger.IsEnabled( Microsoft.Extensions.Logging.LogLevel.Information))
            {
                System.Console.WriteLine(this.RequestId);
            }
        }
    }

}
