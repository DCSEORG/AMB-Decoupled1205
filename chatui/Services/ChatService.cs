using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagementChat.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace ExpenseManagementChat.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(List<ConversationMessage> messages);
}

public class ChatService : IChatService
{
    private readonly GenAISettings _settings;
    private readonly ILogger<ChatService> _logger;
    private readonly HttpClient _httpClient;

    // System prompt describing capabilities and function tools
    private const string SystemPrompt = @"You are an intelligent expense management assistant for ExpenseTracker.
You have access to the following real-time functions to interact with the expense management system:

- get_expenses: Retrieve expenses (optionally filtered by status or user)
- get_expense_by_id: Get details of a specific expense
- create_expense: Create a new expense record
- submit_expense: Submit an expense for manager approval
- approve_expense: Approve a submitted expense
- reject_expense: Reject a submitted expense
- delete_expense: Delete a draft expense
- get_users: Retrieve all users
- get_categories: Retrieve expense categories
- get_summary: Get expense summary statistics

Use these functions when users ask about expense data or want to perform operations.
Format lists using markdown: **bold** for emphasis, - for bullets, 1. for numbered lists.
Always be helpful and provide clear, actionable responses.
Amounts are stored in pence (minor units) - divide by 100 to display in pounds.";

    // Function tool definitions
    private static readonly List<ChatTool> Tools = new()
    {
        ChatTool.CreateFunctionTool(
            "get_expenses",
            "Retrieves expenses from the database, optionally filtered by status or user",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "statusId": {
                  "type": "integer",
                  "description": "Filter by status: 1=Draft, 2=Submitted, 3=Approved, 4=Rejected"
                },
                "userId": {
                  "type": "integer",
                  "description": "Filter by user ID"
                }
              }
            }
            """)),
        ChatTool.CreateFunctionTool(
            "get_expense_by_id",
            "Gets details of a specific expense by its ID",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The expense ID" }
              },
              "required": ["id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "create_expense",
            "Creates a new expense record in the system",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "userId": { "type": "integer", "description": "User ID of the employee" },
                "categoryId": { "type": "integer", "description": "Category ID (1=Travel,2=Meals,3=Supplies,4=Accommodation,5=Other)" },
                "amountMinor": { "type": "integer", "description": "Amount in pence (e.g. 1234 = £12.34)" },
                "expenseDate": { "type": "string", "description": "Date in YYYY-MM-DD format" },
                "description": { "type": "string", "description": "Description of the expense" },
                "statusId": { "type": "integer", "description": "1=Draft, 2=Submitted", "default": 1 }
              },
              "required": ["userId", "categoryId", "amountMinor", "expenseDate"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "submit_expense",
            "Submits a draft expense for manager approval",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The expense ID to submit" }
              },
              "required": ["id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "approve_expense",
            "Approves a submitted expense",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The expense ID to approve" },
                "reviewedBy": { "type": "integer", "description": "User ID of the approving manager" }
              },
              "required": ["id", "reviewedBy"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "reject_expense",
            "Rejects a submitted expense",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The expense ID to reject" },
                "reviewedBy": { "type": "integer", "description": "User ID of the rejecting manager" }
              },
              "required": ["id", "reviewedBy"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "delete_expense",
            "Deletes a draft expense",
            BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The expense ID to delete" }
              },
              "required": ["id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            "get_users",
            "Retrieves all active users in the system",
            BinaryData.FromString("""{"type":"object","properties":{}}""")),
        ChatTool.CreateFunctionTool(
            "get_categories",
            "Retrieves all expense categories",
            BinaryData.FromString("""{"type":"object","properties":{}}""")),
        ChatTool.CreateFunctionTool(
            "get_summary",
            "Gets a summary of expenses grouped by status with counts and totals",
            BinaryData.FromString("""{"type":"object","properties":{}}""")),
    };

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IHttpClientFactory httpClientFactory)
    {
        _settings = configuration.GetSection("GenAISettings").Get<GenAISettings>() ?? new GenAISettings();
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ExpenseApi");
    }

    public async Task<ChatResponse> SendMessageAsync(List<ConversationMessage> messages)
    {
        // If OpenAI is not configured, return dummy response
        if (string.IsNullOrEmpty(_settings.OpenAIEndpoint))
        {
            return new ChatResponse
            {
                Content = "⚠️ **GenAI services are not yet deployed.**\n\nTo enable the AI chat assistant, please run `./deploy-with-chat.sh` which will deploy Azure OpenAI and AI Search resources.\n\nOnce deployed, I can help you:\n- **View expenses** – \"Show me all submitted expenses\"\n- **Create expenses** – \"Create a travel expense for £45 for Alice\"\n- **Approve expenses** – \"Approve expense #3\"\n- **Get summaries** – \"How many expenses are pending approval?\"",
                IsError = false
            };
        }

        try
        {
            // Build credential using ManagedIdentityCredential with explicit client ID
            Azure.Core.TokenCredential credential;
            var managedIdentityClientId = _settings.ManagedIdentityClientId;
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var aoaiClient = new AzureOpenAIClient(new Uri(_settings.OpenAIEndpoint), credential);
            var chatClient = aoaiClient.GetChatClient(_settings.OpenAIModelName ?? "gpt-4o");

            // Build the message list
            var chatMessages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(SystemPrompt)
            };

            foreach (var msg in messages)
            {
                if (msg.Role == "user")
                    chatMessages.Add(new UserChatMessage(msg.Content));
                else if (msg.Role == "assistant")
                    chatMessages.Add(new AssistantChatMessage(msg.Content));
            }

            var options = new ChatCompletionOptions();
            foreach (var tool in Tools)
                options.Tools.Add(tool);

            // Function calling orchestration loop
            while (true)
            {
                var completion = await chatClient.CompleteChatAsync(chatMessages, options);
                var response = completion.Value;

                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add the assistant message with tool calls
                    chatMessages.Add(new AssistantChatMessage(response));

                    // Execute each tool call
                    foreach (var toolCall in response.ToolCalls)
                    {
                        var result = await ExecuteToolCallAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        chatMessages.Add(new ToolChatMessage(toolCall.Id, result));
                    }
                    // Continue the loop to get final response
                }
                else
                {
                    // Final response
                    var content = response.Content[0].Text;
                    return new ChatResponse { Content = content };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatService.SendMessageAsync");
            return new ChatResponse
            {
                Content = $"⚠️ **Chat service error:** {ex.Message}\n\nIf this is a managed identity issue, ensure:\n1. `AZURE_CLIENT_ID` app setting is set to the managed identity client ID\n2. The managed identity has 'Cognitive Services OpenAI User' role on the Azure OpenAI resource\n3. Run `./deploy-with-chat.sh` to configure all settings correctly.",
                IsError = true
            };
        }
    }

    private async Task<string> ExecuteToolCallAsync(string functionName, string arguments)
    {
        try
        {
            _logger.LogInformation("Executing tool: {Function} with args: {Args}", functionName, arguments);
            var args = JsonDocument.Parse(arguments).RootElement;

            return functionName switch
            {
                "get_expenses" => await CallApiAsync("GET", BuildExpenseUrl(args)),
                "get_expense_by_id" => await CallApiAsync("GET", $"/api/expenses/{args.GetProperty("id").GetInt32()}"),
                "create_expense" => await CallApiAsync("POST", "/api/expenses", arguments),
                "submit_expense" => await CallApiAsync("POST", $"/api/expenses/{args.GetProperty("id").GetInt32()}/submit"),
                "approve_expense" => await CallApiAsync("POST", $"/api/expenses/{args.GetProperty("id").GetInt32()}/approve",
                    JsonSerializer.Serialize(new { reviewedBy = args.TryGetProperty("reviewedBy", out var rb) ? rb.GetInt32() : 2 })),
                "reject_expense" => await CallApiAsync("POST", $"/api/expenses/{args.GetProperty("id").GetInt32()}/reject",
                    JsonSerializer.Serialize(new { reviewedBy = args.TryGetProperty("reviewedBy", out var rb2) ? rb2.GetInt32() : 2 })),
                "delete_expense" => await CallApiAsync("DELETE", $"/api/expenses/{args.GetProperty("id").GetInt32()}"),
                "get_users" => await CallApiAsync("GET", "/api/users"),
                "get_categories" => await CallApiAsync("GET", "/api/categories"),
                "get_summary" => await CallApiAsync("GET", "/api/expenses/summary"),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call: {Function}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string BuildExpenseUrl(JsonElement args)
    {
        var url = "/api/expenses";
        var queryParts = new List<string>();
        if (args.TryGetProperty("statusId", out var sid))
            queryParts.Add($"statusId={sid.GetInt32()}");
        if (args.TryGetProperty("userId", out var uid))
            queryParts.Add($"userId={uid.GetInt32()}");
        return queryParts.Any() ? $"{url}?{string.Join("&", queryParts)}" : url;
    }

    private async Task<string> CallApiAsync(string method, string path, string? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body != null)
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
