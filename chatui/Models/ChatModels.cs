namespace ExpenseManagementChat.Models;

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatRequest
{
    public List<ConversationMessage> Messages { get; set; } = new();
}

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class GenAISettings
{
    public string? OpenAIEndpoint { get; set; }
    public string? OpenAIModelName { get; set; }
    public string? SearchEndpoint { get; set; }
    public string? SearchIndexName { get; set; } = "expense-docs";
    public string? ManagedIdentityClientId { get; set; }
}
