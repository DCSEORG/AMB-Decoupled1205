using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<ExpenseSummary> Summary { get; set; } = new();
    public List<Expense> RecentExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public IndexModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        var (summary, summaryError) = await _expenseService.GetExpensesSummaryAsync();
        Summary = summary;
        if (summaryError != null) ErrorMessage = summaryError;

        var (expenses, expensesError) = await _expenseService.GetAllExpensesAsync();
        RecentExpenses = expenses.Take(10).ToList();
        if (expensesError != null && ErrorMessage == null) ErrorMessage = expensesError;
    }
}
