using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

/// <summary>
/// API endpoints for Expense Management operations.
/// All data access goes through stored procedures via the ExpenseService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filters.
    /// </summary>
    /// <param name="statusId">Filter by status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)</param>
    /// <param name="userId">Filter by user ID</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetExpenses([FromQuery] int? statusId = null, [FromQuery] int? userId = null)
    {
        var (expenses, error) = await _expenseService.GetAllExpensesAsync(statusId, userId);
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(expenses);
    }

    /// <summary>
    /// Get a single expense by ID.
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Expense), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetExpense(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        if (error != null) Response.Headers["X-Error"] = error;
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense.
    /// </summary>
    /// <param name="request">Expense creation request</param>
    [HttpPost]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (expenseId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error != null) return StatusCode(500, new { error });
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
    }

    /// <summary>
    /// Update an existing expense (only Draft status).
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Update request</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        var (success, error) = await _expenseService.UpdateExpenseAsync(id, request);
        if (error != null) return StatusCode(500, new { error });
        if (!success) return NotFound();
        return Ok(new { message = "Expense updated successfully." });
    }

    /// <summary>
    /// Submit an expense for approval.
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitExpense(int id)
    {
        var (success, error) = await _expenseService.SubmitExpenseAsync(id);
        if (error != null) return StatusCode(500, new { error });
        if (!success) return NotFound();
        return Ok(new { message = "Expense submitted for approval." });
    }

    /// <summary>
    /// Approve a submitted expense.
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Review request with reviewer's user ID</param>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveExpense(int id, [FromBody] ReviewExpenseRequest request)
    {
        var (success, error) = await _expenseService.ApproveExpenseAsync(id, request.ReviewedBy);
        if (error != null) return StatusCode(500, new { error });
        if (!success) return NotFound();
        return Ok(new { message = "Expense approved." });
    }

    /// <summary>
    /// Reject a submitted expense.
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Review request with reviewer's user ID</param>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RejectExpense(int id, [FromBody] ReviewExpenseRequest request)
    {
        var (success, error) = await _expenseService.RejectExpenseAsync(id, request.ReviewedBy);
        if (error != null) return StatusCode(500, new { error });
        if (!success) return NotFound();
        return Ok(new { message = "Expense rejected." });
    }

    /// <summary>
    /// Delete a draft expense.
    /// </summary>
    /// <param name="id">Expense ID</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var (success, error) = await _expenseService.DeleteExpenseAsync(id);
        if (error != null) return StatusCode(500, new { error });
        if (!success) return NotFound();
        return Ok(new { message = "Expense deleted." });
    }

    /// <summary>
    /// Get summary statistics for expenses.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(List<ExpenseSummary>), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var (summary, error) = await _expenseService.GetExpensesSummaryAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(summary);
    }
}

/// <summary>
/// API endpoints for user lookups.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public UsersController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all active users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Models.User>), 200)]
    public async Task<IActionResult> GetUsers()
    {
        var (users, error) = await _expenseService.GetAllUsersAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(users);
    }
}

/// <summary>
/// API endpoints for reference data (categories and statuses).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public CategoriesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all active expense categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Models.Category>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        var (categories, error) = await _expenseService.GetAllCategoriesAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(categories);
    }
}

/// <summary>
/// API endpoints for expense statuses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public StatusesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Get all expense statuses.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Models.ExpenseStatus>), 200)]
    public async Task<IActionResult> GetStatuses()
    {
        var (statuses, error) = await _expenseService.GetAllStatusesAsync();
        if (error != null) Response.Headers["X-Error"] = error;
        return Ok(statuses);
    }
}
