using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using System.Collections.Generic;
using System.Threading.Tasks;
using Campus_SMS.Data;
using Campus_SMS.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using DocumentFormat.OpenXml.Packaging;
using System.Reflection.PortableExecutable;
using System.Net.Http.Headers;
using System.Text.Json;

public class AiService
{
    private readonly string _apiKey;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;


    public AiService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _apiKey = configuration["OpenAI:_apiKey"];

    }

    public async Task<string> GenerateResponseAsync(string phoneNumber, string studentMessage, string syllabusPath)
    {
        var client = new ChatClient("gpt-4o", _apiKey);

        // Fetch messages from this number to maintain history
        var chatHistory = _context.SmsInteractions
            .Where(s => s.PhoneNumber == phoneNumber)
            .OrderByDescending(s => s.TimeReceived)
            .Take(5) //Limit history
            .ToList();

        // Get all text from syllabus files in the directory
        syllabusPath = ReadSyllabusFiles(syllabusPath);

        // Create a chat request using a list of ChatMessage objects.
        var chatRequest = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant who answers student questions based on course documents that have been uploaded by the professor."),
            new UserChatMessage($"Relevant document context:\n{syllabusPath}"),
        };

        // Add previous messages to context
        foreach (var interaction in chatHistory.OrderBy(s => s.TimeReceived))
        {
            chatRequest.Add(new UserChatMessage(interaction.IncomingSmsMessage));
            chatRequest.Add(new AssistantChatMessage(interaction.AiSmsResponse));
        }

        // Add the new student message
        chatRequest.Add(new UserChatMessage($"Student Question: {studentMessage}"));


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

    // Reads all syllabus files in a directory and extracts text
    private string ReadSyllabusFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return "Error: Syllabus directory not found.";
        }

        var textBuilder = new StringBuilder();

        foreach (var filePath in Directory.GetFiles(directoryPath))
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            string fileText = extension switch
            {
                ".txt" => File.ReadAllText(filePath),
                ".pdf" => ExtractTextFromPdf(filePath),
                ".docx" => ExtractTextFromDocx(filePath),
                _ => ""
            };

            if (!string.IsNullOrWhiteSpace(fileText))
            {
                textBuilder.AppendLine($"--- {System.IO.Path.GetFileName(filePath)} ---\n{fileText}\n");
            }
        }

        return textBuilder.ToString();
    }

    // Extract text from PDFs
    private string ExtractTextFromPdf(string filePath)
    {
        StringBuilder text = new();
        using var reader = new iTextSharp.text.pdf.PdfReader(filePath);
        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
        }
        return text.ToString();
    }

    // Extract text from DOCX
    private string ExtractTextFromDocx(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        return doc.MainDocumentPart.Document.Body.InnerText;
    }
}
