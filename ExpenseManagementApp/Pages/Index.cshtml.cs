using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagementApp.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IndexModel> _logger;

    public bool EnableChatUI { get; set; }

    public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void OnGet()
    {
        EnableChatUI = _configuration.GetValue<bool>("EnableChatUI", true);
    }
}
