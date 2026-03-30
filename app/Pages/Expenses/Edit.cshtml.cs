using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages.Expenses;

public class EditModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public Expense? Expense { get; set; }
    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public EditModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync(int id)
    {
        var (expense, error) = await _expenseService.GetExpenseByIdAsync(id);
        Expense = expense;
        ErrorMessage = error;
        var (categories, _) = await _expenseService.GetAllCategoriesAsync();
        Categories = categories;
    }

    public async Task<IActionResult> OnPostAsync(int id, int CategoryId, decimal AmountGBP, DateTime ExpenseDate, string? Description, string? ReceiptFile)
    {
        var request = new UpdateExpenseRequest
        {
            CategoryId = CategoryId,
            AmountMinor = (int)(AmountGBP * 100),
            Currency = "GBP",
            ExpenseDate = ExpenseDate,
            Description = Description,
            ReceiptFile = ReceiptFile
        };

        var (success, error) = await _expenseService.UpdateExpenseAsync(id, request);
        if (error != null)
        {
            var (categories, _) = await _expenseService.GetAllCategoriesAsync();
            Categories = categories;
            var (expense, _) = await _expenseService.GetExpenseByIdAsync(id);
            Expense = expense;
            ErrorMessage = error;
            return Page();
        }
        return RedirectToPage("/Expenses/Details", new { id });
    }
}
