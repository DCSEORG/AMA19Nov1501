using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagementApp.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;

    public bool IsChatEnabled { get; set; }

    public ChatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        // Check if Azure OpenAI is configured
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        
        IsChatEnabled = !string.IsNullOrEmpty(endpoint) && 
                       !string.IsNullOrEmpty(deploymentName) && 
                       !string.IsNullOrEmpty(apiKey);
    }
}
