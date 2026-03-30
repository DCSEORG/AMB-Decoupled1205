using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<(List<Expense> expenses, string? error)> GetAllExpensesAsync(int? statusId = null, int? userId = null);
    Task<(Expense? expense, string? error)> GetExpenseByIdAsync(int id);
    Task<(int? expenseId, string? error)> CreateExpenseAsync(CreateExpenseRequest request);
    Task<(bool success, string? error)> UpdateExpenseAsync(int id, UpdateExpenseRequest request);
    Task<(bool success, string? error)> SubmitExpenseAsync(int id);
    Task<(bool success, string? error)> ApproveExpenseAsync(int id, int reviewedBy);
    Task<(bool success, string? error)> RejectExpenseAsync(int id, int reviewedBy);
    Task<(bool success, string? error)> DeleteExpenseAsync(int id);
    Task<(List<User> users, string? error)> GetAllUsersAsync();
    Task<(List<Category> categories, string? error)> GetAllCategoriesAsync();
    Task<(List<ExpenseStatus> statuses, string? error)> GetAllStatusesAsync();
    Task<(List<ExpenseSummary> summary, string? error)> GetExpensesSummaryAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(GetConnectionString());
    }

    // ─── GetAllExpenses ────────────────────────────────────────────────
    public async Task<(List<Expense> expenses, string? error)> GetAllExpensesAsync(int? statusId = null, int? userId = null)
    {
        try
        {
            var expenses = new List<Expense>();
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetAllExpenses", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                expenses.Add(MapExpense(reader));
            return (expenses, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllExpensesAsync");
            return (GetDummyExpenses(), BuildError(ex, nameof(GetAllExpensesAsync)));
        }
    }

    // ─── GetExpenseById ───────────────────────────────────────────────
    public async Task<(Expense? expense, string? error)> GetExpenseByIdAsync(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetExpenseById", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (MapExpense(reader), null);
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpenseByIdAsync");
            return (GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == id), BuildError(ex, nameof(GetExpenseByIdAsync)));
        }
    }

    // ─── CreateExpense ────────────────────────────────────────────────
    public async Task<(int? expenseId, string? error)> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.CreateExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@UserId", request.UserId);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency", request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusId", request.StatusId);
            var result = await cmd.ExecuteScalarAsync();
            return (Convert.ToInt32(result), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateExpenseAsync");
            return (null, BuildError(ex, nameof(CreateExpenseAsync)));
        }
    }

    // ─── UpdateExpense ────────────────────────────────────────────────
    public async Task<(bool success, string? error)> UpdateExpenseAsync(int id, UpdateExpenseRequest request)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.UpdateExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency", request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt32(0) > 0, null);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateExpenseAsync");
            return (false, BuildError(ex, nameof(UpdateExpenseAsync)));
        }
    }

    // ─── SubmitExpense ────────────────────────────────────────────────
    public async Task<(bool success, string? error)> SubmitExpenseAsync(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.SubmitExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt32(0) > 0, null);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitExpenseAsync");
            return (false, BuildError(ex, nameof(SubmitExpenseAsync)));
        }
    }

    // ─── ApproveExpense ───────────────────────────────────────────────
    public async Task<(bool success, string? error)> ApproveExpenseAsync(int id, int reviewedBy)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.ApproveExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt32(0) > 0, null);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveExpenseAsync");
            return (false, BuildError(ex, nameof(ApproveExpenseAsync)));
        }
    }

    // ─── RejectExpense ────────────────────────────────────────────────
    public async Task<(bool success, string? error)> RejectExpenseAsync(int id, int reviewedBy)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.RejectExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt32(0) > 0, null);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectExpenseAsync");
            return (false, BuildError(ex, nameof(RejectExpenseAsync)));
        }
    }

    // ─── DeleteExpense ────────────────────────────────────────────────
    public async Task<(bool success, string? error)> DeleteExpenseAsync(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.DeleteExpense", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return (reader.GetInt32(0) > 0, null);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteExpenseAsync");
            return (false, BuildError(ex, nameof(DeleteExpenseAsync)));
        }
    }

    // ─── GetAllUsers ──────────────────────────────────────────────────
    public async Task<(List<User> users, string? error)> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetAllUsers", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    RoleName = reader.GetString(4),
                    ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsActive = reader.GetBoolean(7),
                    CreatedAt = reader.GetDateTime(8)
                });
            }
            return (users, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllUsersAsync");
            return (GetDummyUsers(), BuildError(ex, nameof(GetAllUsersAsync)));
        }
    }

    // ─── GetAllCategories ─────────────────────────────────────────────
    public async Task<(List<Category> categories, string? error)> GetAllCategoriesAsync()
    {
        try
        {
            var categories = new List<Category>();
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetAllCategories", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            return (categories, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllCategoriesAsync");
            return (GetDummyCategories(), BuildError(ex, nameof(GetAllCategoriesAsync)));
        }
    }

    // ─── GetAllStatuses ───────────────────────────────────────────────
    public async Task<(List<ExpenseStatus> statuses, string? error)> GetAllStatusesAsync()
    {
        try
        {
            var statuses = new List<ExpenseStatus>();
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetAllStatuses", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
            return (statuses, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllStatusesAsync");
            return (GetDummyStatuses(), BuildError(ex, nameof(GetAllStatusesAsync)));
        }
    }

    // ─── GetExpensesSummary ───────────────────────────────────────────
    public async Task<(List<ExpenseSummary> summary, string? error)> GetExpensesSummaryAsync()
    {
        try
        {
            var summary = new List<ExpenseSummary>();
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("dbo.GetExpensesSummary", conn) { CommandType = System.Data.CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                summary.Add(new ExpenseSummary
                {
                    StatusName = reader.GetString(0),
                    Count = reader.GetInt32(1),
                    TotalAmountMinor = reader.GetInt32(2)
                });
            }
            return (summary, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetExpensesSummaryAsync");
            return (GetDummySummary(), BuildError(ex, nameof(GetExpensesSummaryAsync)));
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────
    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedByName = reader.IsDBNull(reader.GetOrdinal("ReviewedByName")) ? null : reader.GetString(reader.GetOrdinal("ReviewedByName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private string BuildError(Exception ex, string method)
    {
        var inner = ex.InnerException?.Message ?? "";
        var isManagedIdentityIssue = ex.Message.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("Login failed", StringComparison.OrdinalIgnoreCase);

        var hint = isManagedIdentityIssue
            ? " MANAGED IDENTITY FIX: Ensure (1) the user-assigned identity 'mid-appmodassist-30-11-30' is assigned to the App Service, " +
              "(2) the connection string uses 'Authentication=Active Directory Managed Identity;User Id=<client-id>', " +
              "(3) the identity has been added as a database user via run-sql-dbrole.py, " +
              "(4) AZURE_CLIENT_ID app setting is configured with the identity's client ID."
            : " Hint: Check that the SQL server firewall allows Azure services and your deployment IP.";

        return $"[{method} in ExpenseService.cs] {ex.GetType().Name}: {ex.Message}{hint}";
    }

    // Note: default reviewer ID 2 (Bob Manager from seed data) is used when no reviewer is specified.
    // In production, the logged-in user's ID should be passed instead.

    // ─── Dummy data (shown when DB is unavailable) ─────────────────────
    private static List<Expense> GetDummyExpenses() => new()
    {
        new Expense { ExpenseId = 1, UserId = 1, UserName = "Alice Example (DEMO)", Email = "alice@example.co.uk", CategoryId = 1, CategoryName = "Travel", StatusId = 2, StatusName = "Submitted", AmountMinor = 2540, Currency = "GBP", ExpenseDate = DateTime.Today.AddDays(-10), Description = "Taxi from airport (DEMO DATA - DB unavailable)", CreatedAt = DateTime.UtcNow },
        new Expense { ExpenseId = 2, UserId = 1, UserName = "Alice Example (DEMO)", Email = "alice@example.co.uk", CategoryId = 2, CategoryName = "Meals", StatusId = 3, StatusName = "Approved", AmountMinor = 1425, Currency = "GBP", ExpenseDate = DateTime.Today.AddDays(-25), Description = "Client lunch (DEMO DATA)", CreatedAt = DateTime.UtcNow },
        new Expense { ExpenseId = 3, UserId = 1, UserName = "Alice Example (DEMO)", Email = "alice@example.co.uk", CategoryId = 3, CategoryName = "Supplies", StatusId = 1, StatusName = "Draft", AmountMinor = 799, Currency = "GBP", ExpenseDate = DateTime.Today.AddDays(-5), Description = "Office stationery (DEMO DATA)", CreatedAt = DateTime.UtcNow }
    };

    private static List<User> GetDummyUsers() => new()
    {
        new User { UserId = 1, UserName = "Alice Example (DEMO)", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true, CreatedAt = DateTime.UtcNow },
        new User { UserId = 2, UserName = "Bob Manager (DEMO)", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true, CreatedAt = DateTime.UtcNow }
    };

    private static List<Category> GetDummyCategories() => new()
    {
        new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
        new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
        new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
        new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
        new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
    };

    private static List<ExpenseStatus> GetDummyStatuses() => new()
    {
        new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
        new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
        new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
        new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
    };

    private static List<ExpenseSummary> GetDummySummary() => new()
    {
        new ExpenseSummary { StatusName = "Draft", Count = 1, TotalAmountMinor = 799 },
        new ExpenseSummary { StatusName = "Submitted", Count = 1, TotalAmountMinor = 2540 },
        new ExpenseSummary { StatusName = "Approved", Count = 1, TotalAmountMinor = 1425 },
        new ExpenseSummary { StatusName = "Rejected", Count = 0, TotalAmountMinor = 0 }
    };
}
