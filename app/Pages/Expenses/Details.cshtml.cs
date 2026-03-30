using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages.Expenses;

public class DetailsModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public Expense? Expense { get; set; }
    public string? ErrorMessage { get; set; }

    public DetailsModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        Expense = expense;
        ErrorMessage = error;
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        await _expenseService.SubmitExpenseAsync(id);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, int reviewerId)
    {
        await _expenseService.ApproveExpenseAsync(id, reviewerId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, int reviewerId)
    {
        await _expenseService.RejectExpenseAsync(id, reviewerId);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _expenseService.DeleteExpenseAsync(id);
        return RedirectToPage("/Expenses");
    }
}
