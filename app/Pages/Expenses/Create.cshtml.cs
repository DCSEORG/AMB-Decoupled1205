using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<User> Users { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public CreateModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        await LoadReferenceDataAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        int UserId, int CategoryId, decimal AmountGBP,
        DateTime ExpenseDate, string? Description,
        string? ReceiptFile, int StatusId)
    {
        await LoadReferenceDataAsync();

        var request = new CreateExpenseRequest
        {
            UserId = UserId,
            CategoryId = CategoryId,
            AmountMinor = (int)(AmountGBP * 100),
            Currency = "GBP",
            ExpenseDate = ExpenseDate,
            Description = Description,
            ReceiptFile = ReceiptFile,
            StatusId = StatusId
        };

        var (expenseId, error) = await _expenseService.CreateExpenseAsync(request);
        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }
        return RedirectToPage("/Expenses/Details", new { id = expenseId });
    }

    private async Task LoadReferenceDataAsync()
    {
        var (users, _) = await _expenseService.GetAllUsersAsync();
        Users = users;
        var (categories, _) = await _expenseService.GetAllCategoriesAsync();
        Categories = categories;
    }
}
