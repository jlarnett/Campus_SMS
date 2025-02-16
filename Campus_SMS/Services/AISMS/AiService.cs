using OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AiService
{
    private readonly string _apiKey = "APIKEY"; // Replace with OpenAI API key



    public async Task<string> GenerateResponseAsync(string studentMessage, string syllabusText)
    {
        var client = new ChatClient("gpt-4o", _apiKey);

        // Create a chat request using a list of ChatMessage objects.
        var chatRequest = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant who answers student questions based on course documents."),
            new UserChatMessage($"Student Question: {studentMessage}\n\nRelevant Syllabus Info: {syllabusText}")
        };

        // Call OpenAI to generate a response
        var response = await client.CompleteChatAsync(chatRequest);
        if (response == null || response.Value.Content.Count == 0)
        {
            return "No response generated.";
        }

        var chatResponse = response.Value.Content.Last().Text;

        // Add the chat's response to the chat history
        chatRequest.Add(new AssistantChatMessage(chatResponse));

        return chatResponse;
    }
}
