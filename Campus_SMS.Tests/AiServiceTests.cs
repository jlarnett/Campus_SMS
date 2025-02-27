using Xunit;
using System;
using System.Threading.Tasks;
using Campus_SMS.Data;
using Campus_SMS.Entities;
using Microsoft.EntityFrameworkCore;

public class AiServiceTests
{
    private readonly AiService _aiService;
    private readonly ApplicationDbContext _context;

    public AiServiceTests(AiService aiService)
    {
        // Create options for in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb") // Unique DB per test run
            .Options;

        // Initialize database context
        _context = new ApplicationDbContext(options);

        // Ensure database is created
        _context.Database.EnsureCreated();

        // Pass the context to AiService
        _aiService = aiService;
    }

    [Fact]
    public async Task GenerateResponseAsync_ShouldReturnResponse()
    {
        // Arrange
        string studentMessage = "What is 1+1?";
        // Purposly vague info to prove AI is answering based off syllabus text.
        string syllabusText = "1+1=s(1)+s(1)";
        string phoneNumber = "8127607508";

        try
        {
            // Act
            string response = await _aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusText);

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
