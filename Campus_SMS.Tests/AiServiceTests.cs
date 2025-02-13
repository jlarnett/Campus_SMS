using Xunit;
using System;
using System.Threading.Tasks;

public class AiServiceTests
{
    private readonly AiService _aiService;

    public AiServiceTests()
    {
        _aiService = new AiService();
    }

    [Fact]
    public async Task GenerateResponseAsync_ShouldReturnResponse()
    {
        // Arrange
        string studentMessage = "What is an algorithm?";
        // Purposely wrong material to prove it uses documents to answer questions
        string syllabusText = "An algorithm like an essay for the mind to solve scientific theories.";

        try
        {
            // Act
            string response = await _aiService.GenerateResponseAsync(studentMessage, syllabusText);

            // Log output for debugging
            Console.WriteLine($"AI Response: {response}");

            // Assert
            Assert.False(string.IsNullOrEmpty(response), "AI response should not be empty.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            throw; // Rethrow so xUnit logs the failure properly
        }
    }
}
