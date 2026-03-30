using ExpenseManagementChat.Models;
using ExpenseManagementChat.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagementChat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var response = await _chatService.SendMessageAsync(request.Messages);
        return Ok(response);
    }
}
