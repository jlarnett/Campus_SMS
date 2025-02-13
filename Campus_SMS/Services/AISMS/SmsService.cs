using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using Twilio.Types;
using Campus_SMS.Data;
using Campus_SMS.Entities;

public class SmsService
{
    private readonly string _accountSid = "your_account_sid";
    private readonly string _authToken = "your_auth_token";
    private readonly string _fromPhoneNumber = "your_twilio_phone_number";
    private readonly ApplicationDbContext _context;
    private readonly AiService _aiService;

    public SmsService(ApplicationDbContext context)
    {
        _context = context;
        _aiService = new AiService();
        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task ProcessIncomingMessageAsync(string incomingMessage, string phoneNumber, string syllabusText)
    {
        // Generate AI response based on the incoming message and syllabus
        var aiResponse = await _aiService.GenerateResponseAsync(incomingMessage, syllabusText);

        // Save to database
        SaveSmsInteraction(phoneNumber, incomingMessage, aiResponse);

        // Send response back via Twilio
        await SendSms(phoneNumber, aiResponse);
    }

    // Send SMS
    public async Task SendSms(string toPhoneNumber, string message)
    {
        try
        {
            var messageSent = MessageResource.Create(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_fromPhoneNumber),
                body: message);
        }
        catch (ApiException e)
        {
            Console.WriteLine($"Error sending message: {e.Message}");
        }
    }

    private void SaveSmsInteraction(string phoneNumber, string incomingMessage, string aiResponse)
    {
        // Save interaction to the database
        var smsInteraction = new SmsInteraction
        {
            PhoneNumber = phoneNumber,
            IncomingSmsMessage = incomingMessage,
            AiSmsResponse = aiResponse,
            TimeReceived = DateTime.Now,
            TimeResponded = DateTime.Now //update this later
        };

        _context.SmsInteractions.Add(smsInteraction);
        _context.SaveChanges();
    }
}

