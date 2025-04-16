using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Campus_SMS.Data;
using Campus_SMS.Entities;
using Moq;
using OpenAI.Examples;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace OpenAI.Tests
{
    public class AiServiceVectorStoreTests
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;
        private readonly string _apiKey;
        public List<string> DocIds;

        public AiServiceVectorStoreTests()
        {
            //Twilio Keys go here for testing
            _accountSid = "";
            _authToken = "";
            _fromPhoneNumber = "+";

            //OpenAI API Key
            _apiKey = "";

        }

        private async Task<ApplicationDbContext> DeleteFiles(ApplicationDbContext _dbcontext, string JoinKey)
        {
            // Arrange
            var dbContext = _dbcontext;

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);

            var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);

            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combine the project directory with the relative path to the folder
            string syllabusPath = Path.Combine(projectDirectory, "Documents", "TestFolder");

            string courseKey = JoinKey;

            string directoryPath = syllabusPath;
            Console.WriteLine($"[DELETE FILES] Directory Path: {directoryPath}");

            foreach (var filePath in Directory.GetFiles(directoryPath))
            {
                Console.WriteLine($"[DELETE FILES] Found file: {filePath}");
            }


            var docToDelete = dbContext.OpenAIUploadedDocs
                .Where(d => d.DocumentName == "CIS 333-Syllabi-Spring2025.docx")
                .Select(d => d.DocumentID)
                .ToList();

            // Act
            Console.WriteLine($"[DELETE FILES] START ");

            foreach (var doc in docToDelete)
            {
                Console.WriteLine($"[DELETE FILES] Found {doc}, deleting...");
                await aiService.DeleteDocumentsOpenAI(doc, syllabusPath, courseKey);
            }
            Console.WriteLine($"[DELETE FILES] END ");
            return dbContext;
        }
        private async Task<ApplicationDbContext> GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;

            var dbContext = new ApplicationDbContext(options);
            dbContext.Database.EnsureCreated();

            // Seed test data
            var course = new ClassCourse
            {
                Id = 1,
                ClassDescription = "Test Course",
                UsiClassIdentifier = "TEST101",
                JoinKey = "TESTJOINKEY",
                AssistentId = "asst_7AVGCji6EHy4XjPSRrhy5XYb" //test assistent ID
            };

            var course2 = new ClassCourse
            {
                Id = 2,
                ClassDescription = "Test Course No AI",
                UsiClassIdentifier = "TESTNOAI",
                JoinKey = "TESTJOINKEY2",
                AssistentId = null //test assistent ID
            };

            var smsInteraction = new SmsInteraction
            {
                PhoneNumber = "1234567890",
                IncomingSmsMessage = "What is the syllabus?",
                AiSmsResponse = "The syllabus is available in the course documents.",
                TimeReceived = DateTime.UtcNow
            };

            var uploadedDoc = new OpenAIUploadedDocs
            {
                DocumentName = "CIS282.docx",
                DocumentID = "file-MmvQPMKqfsU4qZsSS2Xpci"
            };

            dbContext.Courses.Add(course);
            dbContext.Courses.Add(course2); // Note: getting "Object reference not set to an instance of an object" error without this. Might also be an error in code.
            dbContext.SmsInteractions.Add(smsInteraction);
            dbContext.OpenAIUploadedDocs.Add(uploadedDoc);

            await dbContext.SaveChangesAsync();
            return dbContext;
        }

        [Fact]
        public async Task SmsServiceTest()
        {
            // Arrange
            var dbContext = await GetDatabaseContext();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Twilio:AccountSID"]).Returns(_accountSid);
            mockConfig.Setup(c => c["Twilio:AuthToken"]).Returns(_authToken);
            mockConfig.Setup(c => c["Twilio:FromPhoneNumber"]).Returns(_fromPhoneNumber);
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);


            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(); // This ensures logging is available
            serviceCollection.AddScoped<SmsService>(sp =>
            {
                // Get dependencies from test.
                var logger = sp.GetRequiredService<ILogger<SmsService>>();
                var _dbContext = dbContext; // your existing dbContext instance
                var config = mockConfig.Object;     // your mock config instance
                var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);
                return new SmsService(_dbContext, config, aiService, logger);
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var smsService = serviceProvider.GetRequiredService<SmsService>();

            string phoneNumber = "+18127607508";
            string studentMessage = "Hello";
            var interactionStartTime = DateTime.UtcNow;
            // Act
            await smsService.ProcessIncomingMessageAsync(studentMessage, phoneNumber, interactionStartTime);

            // Assert
            var message = dbContext.SmsInteractions.FirstOrDefault(m => m.PhoneNumber == "+18127607508");
            Console.WriteLine(message.AiSmsResponse);
            var user = dbContext.SmsUsers.FirstOrDefault(u => u.PhoneNumber == "+18127607508");
            Assert.NotNull(user);
            Assert.False(user.OptStatus);  // New users should not be opted in yet
        }

        //Tests Ai response. with 1 or 2 documents to make sure upload and file check works and 0 or 1 assistance to make sure creation works..
        [Fact]
        public async Task GenerateResponseAsync_AIinDB1Doc()
        {
            // Arrange
            var dbContext = await GetDatabaseContext();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);

            var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);

            string phoneNumber = "1234567890";
            string studentMessage = "[1]What is the syllabus?";
            // Get the path to the project's root directory (one level up from 'bin/Debug/net9.0')
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combine the project directory with the relative path to the folder
            string syllabusPath = Path.Combine(projectDirectory, "Documents", "TestFolder");
            string courseKey = "TESTJOINKEY";

            // Act
            string response = await aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusPath, courseKey);
            Console.WriteLine(response);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response);
            Console.WriteLine($"Generated Response: {response}");
        }

        [Fact]
        public async Task GenerateResponseAsync_AINotinDB1Doc()
        {
            // Arrange
            var dbContext = await GetDatabaseContext();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);

            var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);

            string phoneNumber = "1234567890";
            string studentMessage = "[2]What is the syllabus?";
            // Get the path to the project's root directory (one level up from 'bin/Debug/net9.0')
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combine the project directory with the relative path to the folder
            string syllabusPath = Path.Combine(projectDirectory, "Documents", "TestFolder");
            string courseKey = "TESTJOINKEY2";

            // Act
            string response = await aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusPath, courseKey);
            Console.WriteLine(response);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response);
            Console.WriteLine($"Generated Response: {response}");
        }

        [Fact]
        public async Task GenerateResponseAsync_AIinDB2Doc()
        {
            // Arrange
            var dbContext = await GetDatabaseContext();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);

            var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);

            string phoneNumber = "1234567890";
            string studentMessage = "What is the syllabus?";
            // Get the path to the project's root directory (one level up from 'bin/Debug/net9.0')
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combine the project directory with the relative path to the folder
            string syllabusPath = Path.Combine(projectDirectory, "Documents", "TestFolder2Docs");
            string courseKey = "TESTJOINKEY";

            // Act
            string response = await aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusPath, courseKey);
            Console.WriteLine(response);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response);
            Console.WriteLine($"Generated Response: {response}");
            await DeleteFiles(dbContext, courseKey);
            var deletedDocs = dbContext.OpenAIUploadedDocs.Any(d => d.DocumentName == "CIS 333-Syllabi-Spring2025.docx");
            Assert.False(deletedDocs);
        }

        [Fact]
        public async Task GenerateResponseAsync_AINotinDB2Doc()
        {
            // Arrange
            var dbContext = await GetDatabaseContext();

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAI:RobertAPIKey"]).Returns(_apiKey);

            var aiService = new AiServiceVectorStore(dbContext, mockConfig.Object);

            string phoneNumber = "1234567890";
            string studentMessage = "What is the syllabus?";
            // Get the path to the project's root directory (one level up from 'bin/Debug/net9.0')
            string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combine the project directory with the relative path to the folder
            string syllabusPath = Path.Combine(projectDirectory, "Documents", "TestFolder2Docs");
            string courseKey = "TESTJOINKEY2";

            // Act
            string response = await aiService.GenerateResponseAsync(phoneNumber, studentMessage, syllabusPath, courseKey);
            Console.WriteLine(response);

            // Assert
            Assert.NotNull(response);
            Assert.NotEmpty(response);
            Console.WriteLine($"Generated Response: {response}");
            dbContext = await DeleteFiles(dbContext, courseKey);
            var deletedDocs = dbContext.OpenAIUploadedDocs.Any(d => d.DocumentName == "CIS 333-Syllabi-Spring2025.docx");
            Assert.False(deletedDocs);
        }
    }
}