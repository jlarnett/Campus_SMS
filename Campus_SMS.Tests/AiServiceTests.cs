using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using OpenAI.Examples;
public class AiServiceVectorStoreTests
{
    private readonly AiServiceVectorStore aiService;
    public AiServiceVectorStoreTests(AiServiceVectorStore _aiService) 
    {
        aiService = _aiService;
    }

    [Fact]
    public async Task GenerateResponseAsync_ShouldReturnResponse_WhenCalledWithValidInput()
    {
        // Arrange
        string syllabusPath = @"C:\Users\MahoneyPC\Documents\Campus_SMS\Campus_SMS\Documents\CS282-lFQY\"; // Ensure this directory has valid files
        string phoneNumber = "1234567890"; // Example phone number
        string studentMessage = "Who is instructor?"; // Example student query

        // You need to make sure that the OpenAI API key is set correctly in your environment
        string apiKey = "";
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Fail("OPENAI_API_KEY environment variable is not set.");
            return;
        }

        // Act
        string response = await aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusPath);

        // Assert
        Assert.NotNull(response); // Check if the response is not null
        Assert.NotEmpty(response); // Ensure the response is not empty

        // Optionally, print the response for debugging
        Console.WriteLine($"AI Response: {response}");

        // Optionally, add more checks depending on the response's content.
        Assert.Contains("Did that answer all your questions?", response); // Assuming the AI should end with this phrase
    }
}
