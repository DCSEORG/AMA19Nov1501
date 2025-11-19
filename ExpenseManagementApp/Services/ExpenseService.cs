using Microsoft.Data.SqlClient;
using Azure.Identity;
using ExpenseManagementApp.Models;
using System.Data;

namespace ExpenseManagementApp.Services;

public class ExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;
    private const string SERVER = "sql-expense-mgmt-xyz.database.windows.net";
    private const string DATABASE = "ExpenseManagementDB";

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private SqlConnection GetConnection()
    {
        var managedIdentityClientId = _configuration["MANAGED_IDENTITY_CLIENT_ID"];
        
        string connectionString;
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            connectionString = $"Server=tcp:{SERVER};" +
                             $"Database={DATABASE};" +
                             $"Authentication=Active Directory Managed Identity;" +
                             $"User Id={managedIdentityClientId};";
        }
        else
        {
            // Fallback for local development
            connectionString = $"Server=tcp:{SERVER}," +
                             $"Database={DATABASE};" +
                             $"Encrypt=True;" +
                             $"TrustServerCertificate=False;" +
                             $"Connection Timeout=30;";
        }
        
        return new SqlConnection(connectionString);
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, string? status = null)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT e.*, u.UserName, c.CategoryName, s.StatusName, 
                       reviewer.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users reviewer ON e.ReviewedBy = reviewer.UserId
                WHERE (@UserId IS NULL OR e.UserId = @UserId)
                  AND (@Status IS NULL OR s.StatusName = @Status)
                ORDER BY e.CreatedAt DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT e.*, u.UserName, c.CategoryName, s.StatusName, 
                       reviewer.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users reviewer ON e.ReviewedBy = reviewer.UserId
                WHERE e.ExpenseId = @ExpenseId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching expense {ExpenseId}", expenseId);
            return GetDummyExpenses().FirstOrDefault();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
                VALUES (@UserId, @CategoryId, 1, @AmountMinor, 'GBP', @ExpenseDate, @Description, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.AmountGBP * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return -1;
        }
    }

    public async Task<bool> UpdateExpenseStatusAsync(UpdateExpenseStatusRequest request)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                UPDATE dbo.Expenses 
                SET StatusId = @StatusId,
                    ReviewedBy = @ReviewedBy,
                    ReviewedAt = CASE WHEN @StatusId IN (3, 4) THEN SYSUTCDATETIME() ELSE ReviewedAt END,
                    SubmittedAt = CASE WHEN @StatusId = 2 THEN SYSUTCDATETIME() ELSE SubmittedAt END
                WHERE ExpenseId = @ExpenseId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@StatusId", request.StatusId);
            command.Parameters.AddWithValue("@ReviewedBy", (object?)request.ReviewedBy ?? DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense status");
            return false;
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = "SELECT * FROM dbo.ExpenseCategories WHERE IsActive = 1";
            using var command = new SqlCommand(query, connection);

            var categories = new List<ExpenseCategory>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = "SELECT * FROM dbo.ExpenseStatus";
            using var command = new SqlCommand(query, connection);

            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statuses");
            return GetDummyStatuses();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT u.*, r.RoleName 
                FROM dbo.Users u
                JOIN dbo.Roles r ON u.RoleId = r.RoleId
                WHERE u.IsActive = 1";
            
            using var command = new SqlCommand(query, connection);

            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    ManagerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6),
                    RoleName = reader.GetString(7)
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return GetDummyUsers();
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            ReviewerName = reader.IsDBNull(reader.GetOrdinal("ReviewerName")) ? null : reader.GetString(reader.GetOrdinal("ReviewerName"))
        };
    }

    // Dummy data for error scenarios
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport (Demo Data)",
                UserName = "Demo User",
                CategoryName = "Travel",
                StatusName = "Submitted",
                CreatedAt = DateTime.Now.AddDays(-5)
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, UserName = "Demo Employee", Email = "demo@example.com", RoleId = 1, RoleName = "Employee", IsActive = true, CreatedAt = DateTime.Now },
            new User { UserId = 2, UserName = "Demo Manager", Email = "manager@example.com", RoleId = 2, RoleName = "Manager", IsActive = true, CreatedAt = DateTime.Now }
        };
    }
}
