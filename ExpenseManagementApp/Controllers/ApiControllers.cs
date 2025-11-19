using Microsoft.AspNetCore.Mvc;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public ExpensesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by user ID and status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses([FromQuery] int? userId = null, [FromQuery] string? status = null)
    {
        var expenses = await _expenseService.GetExpensesAsync(userId, status);
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null)
            return NotFound();
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var expenseId = await _expenseService.CreateExpenseAsync(request);
        if (expenseId <= 0)
            return BadRequest("Failed to create expense");
        
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, expenseId);
    }

    /// <summary>
    /// Update expense status (submit, approve, reject)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateExpenseStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
    {
        request.ExpenseId = id;
        var success = await _expenseService.UpdateExpenseStatusAsync(request);
        if (!success)
            return BadRequest("Failed to update expense status");
        
        return NoContent();
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        var request = new UpdateExpenseStatusRequest
        {
            ExpenseId = id,
            StatusId = 2 // Submitted
        };
        var success = await _expenseService.UpdateExpenseStatusAsync(request);
        if (!success)
            return BadRequest("Failed to submit expense");
        
        return NoContent();
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] int reviewedBy)
    {
        var request = new UpdateExpenseStatusRequest
        {
            ExpenseId = id,
            StatusId = 3, // Approved
            ReviewedBy = reviewedBy
        };
        var success = await _expenseService.UpdateExpenseStatusAsync(request);
        if (!success)
            return BadRequest("Failed to approve expense");
        
        return NoContent();
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] int reviewedBy)
    {
        var request = new UpdateExpenseStatusRequest
        {
            ExpenseId = id,
            StatusId = 4, // Rejected
            ReviewedBy = reviewedBy
        };
        var success = await _expenseService.UpdateExpenseStatusAsync(request);
        if (!success)
            return BadRequest("Failed to reject expense");
        
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public CategoriesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        var categories = await _expenseService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public StatusesController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseStatus>>> GetStatuses()
    {
        var statuses = await _expenseService.GetStatusesAsync();
        return Ok(statuses);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ExpenseService _expenseService;

    public UsersController(ExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        var users = await _expenseService.GetUsersAsync();
        return Ok(users);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<OpenAI.Chat.ChatMessage>? ConversationHistory { get; set; }
    }

    /// <summary>
    /// Chat with AI assistant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Chat([FromBody] ChatRequest request)
    {
        var history = request.ConversationHistory ?? new List<OpenAI.Chat.ChatMessage>();
        var response = await _chatService.ChatAsync(request.Message, history);
        
        return Ok(new { response, conversationHistory = history });
    }
}

