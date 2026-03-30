using ExpenseManagementChat.Models;
using ExpenseManagementChat.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Configure HTTP client for Expense API
var expenseApiUrl = builder.Configuration["ExpenseApiUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient("ExpenseApi", client =>
{
    client.BaseAddress = new Uri(expenseApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.Run();
