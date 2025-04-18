using Campus_SMS.Data;
using Campus_SMS.Entities;
using OpenAI.Assistants;
using OpenAI.Files;
using OpenAI.VectorStores;
using System.ClientModel;
using System.Text;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;



namespace OpenAI.Examples
{
    public class AiServiceVectorStore
    {
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;

        public AiServiceVectorStore(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _apiKey = configuration["OpenAI:RobertAPIKey"] ?? string.Empty;
            Console.WriteLine($"[DEBUG] API Key Loaded: {(_apiKey.Length > 0 ? "Yes" : "No")}");
        }

        public async Task<string> GenerateResponseAsync(string phoneNumber, string studentMessage, string syllabusPath, string courseKey)
        {
#pragma warning disable OPENAI001
            Console.WriteLine("[DEBUG] Starting GenerateResponseAsync...");
            OpenAIClient openAIClient = new(_apiKey);
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            AssistantClient assistantClient = openAIClient.GetAssistantClient();
            var vectorStoreClient = openAIClient.GetVectorStoreClient();

            string directoryPath = syllabusPath;
            Console.WriteLine($"[DEBUG] Directory Path: {directoryPath}");
            OpenAIFile file = null;

            bool newFileExists = false;

            if (!Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            // Upload files (or reuse existing ones) for vector store creation
            foreach (string filePath in Directory.GetFiles(directoryPath))
            {
                string fileName = Path.GetFileName(filePath);
                Console.WriteLine($"[DEBUG] Checking file: {fileName} (Full Path: {filePath})");

                // Use only the file name for DB checks
                newFileExists = !_context.OpenAIUploadedDocs.Any(f => f.DocumentName == fileName && f.CourseFolder == courseKey);
                Console.WriteLine($"[DEBUG] New File exists? {newFileExists}");
                break;
            }

            var course = _context.Courses.FirstOrDefault(c => c.JoinKey == courseKey);
            var existingAssistant = course.AssistentId;
            var assistentId = "";
            if (existingAssistant != null)
            {
                if (newFileExists)
                {
                    Console.WriteLine("[DEBUG] New files detected. Recreating assistant...");
                    // delete the old assistan
                    try
                    {
                        Console.WriteLine("[DEBUG] Deleting old assistant");

                        OpenAI.Assistants.Assistant assit = assistantClient.GetAssistant(course.AssistentId);
                        var vectorIds = assit.ToolResources
                                    .FileSearch
                                    .VectorStoreIds;

                        // get the first (and in my case only) store ID:
                        string vectorStoreId = vectorIds.FirstOrDefault();
                        if (vectorStoreId == null)
                        {
                            vectorStoreId = "vs-default";
                        }

                        await assistantClient.DeleteAssistantAsync(course.AssistentId);
                        //Delete the vector store
                        try
                        {
                            Console.WriteLine($"[DEBUG] Deleting vector store {vectorStoreId}.");
                            await vectorStoreClient.DeleteVectorStoreAsync(vectorStoreId);
                            Console.WriteLine($"[DEBUG] Successfully deleted vector store {vectorStoreId}");
                        }
                        catch (ClientResultException ex) when (ex.Status == 404)
                        {
                            Console.WriteLine($"[WARN] Vector store {vectorStoreId} not found (already deleted?): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to delete vector store {vectorStoreId}: {ex.Message}");
                            throw;
                        }

                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("No assistant found with id") || ex.Message.Contains("Value cannot be null. (Parameter 'assistantId')"))
                        {
                            Console.WriteLine("[DEBUG] Assistant already deleted.");
                        }
                        else
                        {
                            // Log or rethrow other exceptions
                            Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                            throw;
                        }
                    }
                    assistentId = await CreateAssistent(courseKey, directoryPath);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Assistent already exists and requires no update: " + course.AssistentId);
                    assistentId = course.AssistentId;
                }

            }
            else
            {
                Console.WriteLine("[DEBUG] No Assistent detected. Creating assistant...");
                assistentId = await CreateAssistent(courseKey, directoryPath);
            }

            // Fetch messages from this number to maintain history
            var chatHistory = _context.SmsInteractions
                .Where(s => s.PhoneNumber == phoneNumber)
                .OrderByDescending(s => s.TimeReceived)
                .Take(5) //Limit history
                .ToList();

            // Create a thread with the student's and chat bot's past 5 message and wait for the response.
            Console.WriteLine($"[DEBUG] Creating thread with student's message: {studentMessage}");

            var stringBuilder = new StringBuilder();
            foreach (var interaction in chatHistory)
            {
                stringBuilder.AppendLine($"Role: User, Message: {interaction.IncomingSmsMessage};");
                stringBuilder.AppendLine($"Role: System, Message: {interaction.AiSmsResponse};");
            }

            stringBuilder.AppendLine($"Role: User, Message: {studentMessage} Use documents provided.;");

            ThreadCreationOptions threadOptions = new()
            {
                InitialMessages = { stringBuilder.ToString() },
            };

            ThreadRun threadRun = null; // Declare threadRun outside try-catch

            try
            {
                threadRun = await assistantClient.CreateThreadAndRunAsync(assistentId, threadOptions);
                Console.WriteLine($"[DEBUG] Thread created. Thread ID: {threadRun.ThreadId}, Run ID: {threadRun.Id}");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("No assistant found with id") || ex.Message.Contains("Value cannot be null. (Parameter 'assistantId')"))
                {
                    Console.WriteLine("[DEBUG] Assistant not found. Creating a new assistant...");

                    // Create a new assistant
                    var newAssistant = await CreateAssistent(courseKey, directoryPath);

                    // Retry thread creation with new assistant ID
                    threadRun = await assistantClient.CreateThreadAndRunAsync(newAssistant, threadOptions);
                    Console.WriteLine($"[DEBUG] Thread created with new assistant. Thread ID: {threadRun.ThreadId}, Run ID: {threadRun.Id}");
                }
                else
                {
                    // Log or rethrow other exceptions
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    throw;
                }
            }

            // Wait for thread run to complete.
            Console.WriteLine("[DEBUG] Waiting for thread run to complete...");
            do
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1));
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

        public async Task<string> CreateAssistent(string courseKey, string directoryPath)
        {
            var course = _context.Courses.FirstOrDefault(c => c.JoinKey == courseKey);
            OpenAIClient openAIClient = new(_apiKey);
            AssistantClient assistantClient = openAIClient.GetAssistantClient();
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            var vectorStoreClient = openAIClient.GetVectorStoreClient();
            OpenAIFile file = null;
            List<string> fileIds = new(); 

            //Delete old assistent if one exists
            try
            {
                Console.WriteLine("[DEBUG] Deleting old assistant");
                OpenAI.Assistants.Assistant assit = assistantClient.GetAssistant(course.AssistentId);

                var vectorIds = assit.ToolResources
                            .FileSearch
                            .VectorStoreIds;


                // get the first (and in my case only) store ID:
                string vectorStoreId = vectorIds.FirstOrDefault();
                if (vectorStoreId == null)
                {
                    vectorStoreId = "vs-default";
                }
                await assistantClient.DeleteAssistantAsync(course.AssistentId);

                //Delete the vector store
                try
                {
                    Console.WriteLine($"[DEBUG] Deleting vector store {vectorStoreId}.");
                    await vectorStoreClient.DeleteVectorStoreAsync(vectorStoreId);
                    Console.WriteLine($"[DEBUG] Successfully deleted vector store {vectorStoreId}");
                }
                catch (ClientResultException ex) when (ex.Status == 404)
                {
                    Console.WriteLine($"[WARN] Vector store {vectorStoreId} not found (already deleted?): {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to delete vector store {vectorStoreId}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("No assistant found with id") || ex.Message.Contains("Value cannot be null. (Parameter 'assistantId')"))
                {
                    Console.WriteLine("[DEBUG] Assistant either is already deleted or has not been created yet.");
                }
                else
                {
                    // Log or rethrow other exceptions
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    throw;
                }
            }
            if (!Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            string courseFolderName = new DirectoryInfo(directoryPath).Name;

            //uplaod files if needed
            foreach (string filePath in Directory.GetFiles(directoryPath))
            {
                string fileName = Path.GetFileName(filePath);
                Console.WriteLine($"[DEBUG] Checking file: {fileName} (Full Path: {filePath})");

                var existingDoc = _context.OpenAIUploadedDocs.FirstOrDefault(f => f.DocumentName == fileName && f.CourseFolder == courseFolderName);
                if (existingDoc != null)
                {
                    Console.WriteLine($"[DEBUG] Existing File ID for {fileName}: {existingDoc.DocumentID}");
                    fileIds.Add(existingDoc.DocumentID);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] New file {fileName} detected");
                    Console.WriteLine($"[DEBUG] Uploading file: {fileName}...");
                    using FileStream fileStream = File.OpenRead(filePath);
                    file = await fileClient.UploadFileAsync(fileStream, fileName, FileUploadPurpose.Assistants);
                    Console.WriteLine($"[DEBUG] Uploaded File ID: {file.Id}");

                    var document = new OpenAIUploadedDocs
                    {
                        DocumentName = fileName,
                        DocumentID = file.Id,
                        CourseFolder = courseFolderName
                    };
                    _context.OpenAIUploadedDocs.Add(document);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[DEBUG] Stored {fileName} in DB with ID: {file.Id}");
                    fileIds.Add(file.Id);
                }
            }
            // Create or reuse the assistant without deleting it afterward.
            AssistantCreationOptions assistantOptions = new()
            {
                Name = course.UsiClassIdentifier + " Teachers Assistant",
                Instructions = "You are a helpful assistant who answers student questions based on course documents uploaded by the professor. " +
                            "Keep each response under 1600 characters and do not include source citations. " +
                            "After each answer, end with: 'Let me know if you're done asking questions?' " +
                            "If the student replies affirmatively (e.g., 'yes', 'yep', 'done'), your next message should be exactly: D1o0N78e — nothing else. " +
                            "If the student replies negatively, instruct them to email their professor. " +
                            "Otherwise, continue answering their questions." +
                            "If message is a course code (e.g., ENG101, CIS201, ECON 375) Simply reply you are ready to ansewer question on that course"+
                            "If the student seems to be struggling, confused, or repeatedly asking about the same thing 3 times,"+
                            "offer to escalate the question to their professor."+
                            "If escalation is accepted, respond with: 'ESCALATE-REQ' to flag the request in the system",


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

            // create
            Assistants.Assistant assistant = assistantClient.CreateAssistant("gpt-4o-mini", assistantOptions);
            Console.WriteLine($"[DEBUG] Assistant Created. ID: {assistant.Id}");

            course.AssistentId = assistant.Id;
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Stored assistent in DB with ID: {assistant.Id}");
            return assistant.Id;
        }

        public async System.Threading.Tasks.Task DeleteAssistent(string assistantId)
        {
            OpenAIClient openAIClient = new(_apiKey);
            AssistantClient assistantClient = openAIClient.GetAssistantClient();
            var vectorStoreClient = openAIClient.GetVectorStoreClient();
            

            //Delete old assistent if one exists
            try
            {
                Console.WriteLine("[DEBUG] Deleting assistant");
                OpenAI.Assistants.Assistant assit = assistantClient.GetAssistant(assistantId);
                var vectorIds = assit.ToolResources
                            .FileSearch
                            .VectorStoreIds;

                // get the first (and in my case only) store ID:
                string vectorStoreId = vectorIds.FirstOrDefault();
                if (vectorStoreId == null)
                {
                    vectorStoreId = "vs-default";
                }
                await assistantClient.DeleteAssistantAsync(assistantId);

                //Delete the vector store
                try
                {
                    Console.WriteLine($"[DEBUG] Deleting vector store {vectorStoreId}.");
                    await vectorStoreClient.DeleteVectorStoreAsync(vectorStoreId);
                    Console.WriteLine($"[DEBUG] Successfully deleted vector store {vectorStoreId}");
                }
                catch (ClientResultException ex) when (ex.Status == 404)
                {
                    Console.WriteLine($"[WARN] Vector store {vectorStoreId} not found (already deleted?): {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to delete vector store {vectorStoreId}: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("No assistant found with id") || ex.Message.Contains("Value cannot be null. (Parameter 'assistantId')"))
                {
                    Console.WriteLine("[DEBUG] Assistant either is already deleted or has not been created yet.");
                }
                else
                {
                    // Log or rethrow other exceptions
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    throw;
                }
            }
        }

        public async System.Threading.Tasks.Task DeleteDocumentsOpenAI(string DocId, string CourceDocumentsPath, string JoinKey)
        {
            OpenAIClient openAIClient = new(_apiKey);
            OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
            AssistantClient assistantClient = openAIClient.GetAssistantClient();
            var vectorStoreClient = openAIClient.GetVectorStoreClient();
            OpenAIFile file = null;

            try
            {
                await fileClient.DeleteFileAsync(DocId);
            }
            catch (Exception ex) 
            { 
                if(ex.Message.Contains("No such File object"))
                {
                    Console.WriteLine($"[DEBUG] No such file with id {DocId} exist in OpenAI Storage, skipping.");
                }
                else
                {
                    // Log or rethrow other exceptions
                    Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                    throw;
                }
            }

            var doc = _context.OpenAIUploadedDocs.FirstOrDefault(d => d.DocumentID == DocId);
            if (doc != null)
            {
                _context.OpenAIUploadedDocs.Remove(doc);
                await _context.SaveChangesAsync();
            }

            string directoryPath = CourceDocumentsPath;
            Console.WriteLine($"[DEBUG] Directory Path: {directoryPath}");
            List<string> fileIds = new();

            var course = _context.Courses.FirstOrDefault(c => c.JoinKey == JoinKey);

            if (course != null)
            {
                //Delete Old Client
                Console.WriteLine("[DEBUG] Deleting old assistant");
                //Delete old assistent if one exists
                try
                {
                    Console.WriteLine("[DEBUG] Deleting assistant");
                    OpenAI.Assistants.Assistant assit = assistantClient.GetAssistant(course.AssistentId);
                    var vectorIds = assit.ToolResources
                                .FileSearch
                                .VectorStoreIds;
                    
                    // get the first (and in my case only) store ID:
                    string vectorStoreId = vectorIds.FirstOrDefault();
                    
                    if(vectorStoreId == null)
                    {
                        vectorStoreId = "vs-default";
                    }

                    await assistantClient.DeleteAssistantAsync(course.AssistentId);

                    //Delete the vector store
                    
                    try
                    {
                        Console.WriteLine($"[DEBUG] Deleting vector store {vectorStoreId}.");
                        await vectorStoreClient.DeleteVectorStoreAsync(vectorStoreId);
                        Console.WriteLine($"[DEBUG] Successfully deleted vector store {vectorStoreId}");
                    }
                    catch (ClientResultException ex) when (ex.Status == 404)
                    {
                        Console.WriteLine($"[WARN] Vector store {vectorStoreId} not found (already deleted?): {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to delete vector store {vectorStoreId}: {ex.Message}");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("No assistant found with id") || ex.Message.Contains("Value cannot be null. (Parameter 'assistantId')"))
                    {
                        Console.WriteLine("[DEBUG] Assistant either is already deleted or has not been created yet.");
                    }
                    else
                    {
                        // Log or rethrow other exceptions
                        Console.WriteLine($"[ERROR] Unexpected error: {ex.Message}");
                        throw;
                    }
                }

            }
            else 
            {
                Console.WriteLine("[ERROR] No course found from which the files are being deleted");
            }

            // Recreate assistant after deleting file.
            await CreateAssistent(JoinKey, CourceDocumentsPath);
        }
    }
}
