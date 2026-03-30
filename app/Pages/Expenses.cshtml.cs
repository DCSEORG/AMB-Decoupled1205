using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public List<Expense> Expenses { get; set; } = new();
    public List<Models.User> Users { get; set; } = new();
    public List<Models.ExpenseStatus> Statuses { get; set; } = new();
    public int? SelectedStatusId { get; set; }
    public int? SelectedUserId { get; set; }
    public string? ErrorMessage { get; set; }

    public ExpensesModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public async Task OnGetAsync(int? statusId = null, int? userId = null)
    {
        SelectedStatusId = statusId;
        SelectedUserId = userId;

        var (expenses, err1) = await _expenseService.GetAllExpensesAsync(statusId, userId);
        Expenses = expenses;
        if (err1 != null) ErrorMessage = err1;

        var (users, _) = await _expenseService.GetAllUsersAsync();
        Users = users;

        var (statuses, _) = await _expenseService.GetAllStatusesAsync();
        Statuses = statuses;
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        await _expenseService.SubmitExpenseAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, int reviewerId)
    {
        await _expenseService.ApproveExpenseAsync(id, reviewerId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, int reviewerId)
    {
        await _expenseService.RejectExpenseAsync(id, reviewerId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _expenseService.DeleteExpenseAsync(id);
        return RedirectToPage();
    }
}
