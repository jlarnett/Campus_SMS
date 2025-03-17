using Campus_SMS.Data;
using Campus_SMS.Entities;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using Microsoft.AspNetCore.Routing;
using Microsoft.Graph.Models;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Files;
using OpenAI.VectorStores;
using System;
using System.ClientModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.Examples
{
    public class AiServiceVectorStore
    {
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;

        public AiServiceVectorStore(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _apiKey = configuration.GetValue<string>("OpenAI:RobertAPIKey") ?? string.Empty;
            Console.WriteLine($"[DEBUG] API Key Loaded: {(_apiKey.Length > 0 ? "Yes" : "No")}");
        }

        public async Task<string> GenerateResponseAsync(string phoneNumber, string studentMessage, string syllabusPath, string courseKey)
        {
#pragma warning disable OPENAI001
            Console.WriteLine("[DEBUG] Starting GenerateResponseAsync...");
            OpenAIClient openAIClient = new(_apiKey);
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            AssistantClient assistantClient = openAIClient.GetAssistantClient();

            string directoryPath = syllabusPath;
            Console.WriteLine($"[DEBUG] Directory Path: {directoryPath}");
            List<string> fileIds = new();
            OpenAIFile file = null;

            bool newFileExists = false;
            // Upload files (or reuse existing ones) for vector store creation
            foreach (string filePath in Directory.GetFiles(directoryPath))
            {
                string fileName = Path.GetFileName(filePath);
                Console.WriteLine($"[DEBUG] Checking file: {fileName} (Full Path: {filePath})");

                // Use only the file name for DB checks
                bool fileExists = _context.OpenAIUploadedDocs.Any(f => f.DocumentName == fileName);
                Console.WriteLine($"[DEBUG] File exists in DB? {fileExists}");

                if (!fileExists)
                {
                    newFileExists = true;
                    Console.WriteLine($"[DEBUG] Uploading file: {fileName}...");
                    using FileStream fileStream = File.OpenRead(filePath);
                    file = await fileClient.UploadFileAsync(fileStream, fileName, FileUploadPurpose.Assistants);
                    Console.WriteLine($"[DEBUG] Uploaded File ID: {file.Id}");

                    var document = new OpenAIUploadedDocs
                    {
                        DocumentName = fileName,  // store only file name
                        DocumentID = file.Id
                    };
                    _context.OpenAIUploadedDocs.Add(document);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[DEBUG] Stored {fileName} in DB with ID: {file.Id}");
                    fileIds.Add(file.Id);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] File already exists in database: {fileName}. Retrieving existing file ID.");
                    var existingDoc = _context.OpenAIUploadedDocs.FirstOrDefault(f => f.DocumentName == fileName);
                    if (existingDoc != null)
                    {
                        Console.WriteLine($"[DEBUG] Existing File ID for {fileName}: {existingDoc.DocumentID}");
                        fileIds.Add(existingDoc.DocumentID);
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] File {fileName} exists in DB but could not retrieve its ID.");
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Total file IDs for vector store: {fileIds.Count}");
            foreach (var id in fileIds)
            {
                Console.WriteLine($"[DEBUG] File ID: {id}");
            }

            var course = _context.Courses.FirstOrDefault(c => c.JoinKey == courseKey);
            var existingAssistant = course.AssistentId;
            var assistentId = "";
            if (existingAssistant != null)
            {
                if (newFileExists)
                {
                    Console.WriteLine("[DEBUG] New files detected. Recreating assistant...");
                    // Optionally delete the old assistant if desired:
                    await assistantClient.DeleteAssistantAsync(course.AssistentId);

                    // Create or reuse the assistant without deleting it afterward.
                    AssistantCreationOptions assistantOptions = new()
                    {
                        Name = course.UsiClassIdentifier + " Teachers Assistant",
                        Instructions = "You are a helpful assistant who answers student questions based on course documents that have been " +
                                       "uploaded by the professor. After providing each answer, end with 'Did that answer all your questions?' " +
                                       "If the student replies affirmatively (for example, by saying 'yes', 'yep', 'done' or similar), " +
                                       "then in your next message respond with exactly 'D1o0N78e' and nothing else. " +
                                       "If the student replies negatively, instruct them to email their professor." +
                                       "Otherwise, continue answering further questions. Please keep your responses under 1600 characters." +
                                       "Do not include source citation.",

                        Tools =
                        {
                            new FileSearchToolDefinition(),
                            //new CodeInterpreterToolDefinition(),
                        },
                        ToolResources = new()
                        {
                            FileSearch = new()
                            {
                                NewVectorStores =
                                {
                                    new VectorStoreCreationHelper(fileIds),
                                }
                            }
                        }
                    };

                    Console.WriteLine("[DEBUG] Creating assistant with options:");
                    Console.WriteLine($"[DEBUG] Assistant Name: {assistantOptions.Name}");
                    Console.WriteLine($"[DEBUG] Vector Store File IDs: {string.Join(", ", fileIds)}");

                    course.AssistentId = null;
                    await _context.SaveChangesAsync();

                    // recreate
                    Assistant assistant = assistantClient.CreateAssistant("gpt-4o-mini", assistantOptions);
                    Console.WriteLine($"[DEBUG] Assistant Created. ID: {assistant.Id}");
                    course.AssistentId = assistant.Id;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[DEBUG] Stored assistent in DB with ID: {assistant.Id}");
                    assistentId = assistant.Id;
                }
                else
                {
                    Console.WriteLine("[DEBUG] Assistent already exists and requires no update: " + course.AssistentId);
                    assistentId = course.AssistentId;
                }
                
            }
            else
            {
                // Create or reuse the assistant without deleting it afterward.
                AssistantCreationOptions assistantOptions = new()
                {
                    Name = course.UsiClassIdentifier + " Teachers Assistant",
                    Instructions = "You are a helpful assistant who answers student questions based on course documents that have been " +
                                       "uploaded by the professor. After providing each answer, end with 'Did that answer all your questions?' " +
                                       "If the student replies affirmatively (for example, by saying 'yes', 'yep', 'done' or similar), " +
                                       "then in your next message respond with exactly 'D1o0N78e' and nothing else. " +
                                       "If the student replies negatively, instruct them to email their professor." +
                                       "Otherwise, continue answering further questions. Please keep your responses under 1600 characters." +
                                       "Do not include source citation.",
                    Tools =
                {
                    new FileSearchToolDefinition(),
                    //new CodeInterpreterToolDefinition(),
                },
                    ToolResources = new()
                    {
                        FileSearch = new()
                        {
                            NewVectorStores =
                            {
                                new VectorStoreCreationHelper(fileIds)
                            }
                        }
                    },
                };

                Console.WriteLine("[DEBUG] Creating assistant with options:");
                Console.WriteLine($"[DEBUG] Assistant Name: {assistantOptions.Name}");
                Console.WriteLine($"[DEBUG] Vector Store File IDs: {string.Join(", ", fileIds)}");

                // IMPORTANT: Create the assistant once and reuse it in future queries if possible.
                Assistant assistant = assistantClient.CreateAssistant("gpt-4o-mini", assistantOptions);
                Console.WriteLine($"[DEBUG] Assistant Created. ID: {assistant.Id}");
                course.AssistentId = assistant.Id;
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] Stored assistent in DB with ID: {assistant.Id}");
                assistentId = assistant.Id;
            }

            // Fetch messages from this number to maintain history
            var chatHistory = _context.SmsInteractions
                .Where(s => s.PhoneNumber == phoneNumber)
                .OrderByDescending(s => s.TimeReceived)
                .Take(5) //Limit history
                .ToList();

            // Create a thread with the student's message and wait for the response.
            Console.WriteLine($"[DEBUG] Creating thread with student's message: {studentMessage}");

            // Add previous messages to context
            var messagesHistory = "";
            foreach (var interaction in chatHistory.OrderBy(s => s.TimeReceived))
            {
                messagesHistory += "Role: User, Message: " + interaction.IncomingSmsMessage+";";
                messagesHistory += "Role: System, Message: " + interaction.AiSmsResponse+";";
            }

            ThreadCreationOptions threadOptions = new()
            {
                InitialMessages = { messagesHistory + "Role: User, Message: " + studentMessage+ " Use documents provided.;" },
            };

            ThreadRun threadRun = await assistantClient.CreateThreadAndRunAsync(assistentId, threadOptions);
            Console.WriteLine($"[DEBUG] Thread created. Thread ID: {threadRun.ThreadId}, Run ID: {threadRun.Id}");

            // Wait for thread run to complete.
            Console.WriteLine("[DEBUG] Waiting for thread run to complete...");
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                threadRun = await assistantClient.GetRunAsync(threadRun.ThreadId, threadRun.Id);
                Console.WriteLine($"[DEBUG] Thread run status: {threadRun.Status}");
            } while (!threadRun.Status.IsTerminal);
            Console.WriteLine("[DEBUG] Thread run completed.");

            // Retrieve and process the AI's response.
            Console.WriteLine("[DEBUG] Retrieving thread messages...");
            CollectionResult<ThreadMessage> messages = assistantClient.GetMessages(threadRun.ThreadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Ascending });
            Console.WriteLine($"[DEBUG] Retrieved {messages.Count()} messages from thread.");

            string response = "";
            bool isFirstMessage = true;
            foreach (ThreadMessage message in messages)
            {
                Console.WriteLine($"[DEBUG] Processing message from {message.Role}");
                if (isFirstMessage)
                {
                    isFirstMessage = false;
                    Console.WriteLine("[DEBUG] Skipping first message (student's input).");
                    continue;
                }
                foreach (MessageContent contentItem in message.Content)
                {
                    if (!string.IsNullOrEmpty(contentItem.Text))
                    {
                        Console.WriteLine($"[DEBUG] Message content: {contentItem.Text}");
                        response += contentItem.Text + "\n";
                    }
                }
            }

            // Efficiently delete only the thread so that the assistant and files remain for reuse.
            Console.WriteLine("[DEBUG] Cleaning up: Deleting thread only...");
            await assistantClient.DeleteThreadAsync(threadRun.ThreadId);
            // Note: We are NOT deleting the assistant or the files.
            Console.WriteLine("[DEBUG] Cleanup complete.");

            // Ensure the response is within 1600 characters and removes citation.
            Console.WriteLine($"[DEBUG] Final response length before truncation: {response.Length} characters.");
            response = response.Length > 1600 ? response.Substring(0, 1599) : response;
            response = Regex.Replace(response, "【.*?】", "");
            Console.WriteLine("[DEBUG] Final response generated.");
            Console.WriteLine($"[DEBUG] Response:\n{response}");

            return response.Trim();
        }
    }
}
