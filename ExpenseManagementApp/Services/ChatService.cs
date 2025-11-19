using Azure.AI.OpenAI;
using Azure;
using ExpenseManagementApp.Models;
using System.Text.Json;
using OpenAI.Chat;

namespace ExpenseManagementApp.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly ExpenseService _expenseService;
    private AzureOpenAIClient? _openAIClient;
    private ChatClient? _chatClient;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, ExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        InitializeClient();
    }

    private void InitializeClient()
    {
        try
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
            var apiKey = _configuration["AzureOpenAI:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Azure OpenAI configuration not found. Chat features will be unavailable.");
                return;
            }

            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _chatClient = _openAIClient.GetChatClient(deploymentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
        }
    }

    public async Task<string> ChatAsync(string userMessage, List<ChatMessage> conversationHistory)
    {
        if (_chatClient == null)
        {
            return "Chat service is not available. Azure OpenAI configuration is missing.";
        }

        try
        {
            // Build system prompt with context
            var systemPrompt = @"You are an AI assistant for an Expense Management System. You can help users:
1. View their expenses
2. Create new expenses
3. Check expense status
4. Get summaries and insights
5. Answer questions about the expense system

When users ask about expenses, you can call the available functions to get real data.
Be helpful, concise, and professional.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };
            
            // Add conversation history
            messages.AddRange(conversationHistory);
            
            // Add current user message
            messages.Add(new UserChatMessage(userMessage));

            // Define available functions
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    functionName: "get_expenses",
                    functionDescription: "Get list of expenses for a user",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId": {
                                "type": "integer",
                                "description": "The user ID to filter expenses"
                            },
                            "status": {
                                "type": "string",
                                "description": "Filter by status (Draft, Submitted, Approved, Rejected)"
                            }
                        }
                    }
                    """)
                ),
                ChatTool.CreateFunctionTool(
                    functionName: "create_expense",
                    functionDescription: "Create a new expense",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "userId": {
                                "type": "integer",
                                "description": "The user ID"
                            },
                            "categoryId": {
                                "type": "integer",
                                "description": "Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)"
                            },
                            "amountGBP": {
                                "type": "number",
                                "description": "Amount in GBP"
                            },
                            "description": {
                                "type": "string",
                                "description": "Description of the expense"
                            }
                        },
                        "required": ["userId", "categoryId", "amountGBP"]
                    }
                    """)
                )
            };

            var chatOptions = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                chatOptions.Tools.Add(tool);
            }

            var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);
            var responseMessage = completion.Value.Content[0].Text;

            // Handle function calls if present
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                foreach (var toolCall in completion.Value.ToolCalls)
                {
                    var functionCall = toolCall as ChatToolCall;
                    if (functionCall != null)
                    {
                        var functionResult = await ExecuteFunctionAsync(functionCall.FunctionName, functionCall.FunctionArguments);
                        messages.Add(new AssistantChatMessage(completion.Value));
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                        
                        // Get final response with function results
                        var finalCompletion = await _chatClient.CompleteChatAsync(messages);
                        responseMessage = finalCompletion.Value.Content[0].Text;
                    }
                }
            }

            return responseMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat service");
            return "I'm having trouble processing your request. Please try again later.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData arguments)
    {
        try
        {
            var argsJson = arguments.ToString();
            _logger.LogInformation("Executing function: {FunctionName} with args: {Arguments}", functionName, argsJson);

            switch (functionName)
            {
                case "get_expenses":
                    var getExpensesArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
                    var userId = getExpensesArgs?.ContainsKey("userId") == true ? getExpensesArgs["userId"].GetInt32() : (int?)null;
                    var status = getExpensesArgs?.ContainsKey("status") == true ? getExpensesArgs["status"].GetString() : null;
                    
                    var expenses = await _expenseService.GetExpensesAsync(userId, status);
                    return JsonSerializer.Serialize(expenses.Take(10)); // Limit to 10 for context

                case "create_expense":
                    var createExpenseArgs = JsonSerializer.Deserialize<CreateExpenseRequest>(argsJson);
                    if (createExpenseArgs != null)
                    {
                        createExpenseArgs.ExpenseDate = DateTime.Now;
                        var expenseId = await _expenseService.CreateExpenseAsync(createExpenseArgs);
                        return JsonSerializer.Serialize(new { success = expenseId > 0, expenseId });
                    }
                    return JsonSerializer.Serialize(new { success = false, error = "Invalid arguments" });

                default:
                    return JsonSerializer.Serialize(new { error = "Unknown function" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
