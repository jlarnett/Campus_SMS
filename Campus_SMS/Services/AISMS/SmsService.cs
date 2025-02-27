using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using Twilio.Types;
using Campus_SMS.Data;
using Campus_SMS.Entities;


public class SmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;
    private readonly ApplicationDbContext _context;
    private readonly AiService _aiService;
    private readonly IConfiguration _configuration;

    public SmsService(ApplicationDbContext context, IConfiguration configuration, AiService aiService)
    {
        _accountSid = configuration["Twilio:_accountSid"];
        _authToken = configuration["Twilio:_authToken"];
        _fromPhoneNumber = configuration["Twilio:_fromPhoneNumber"];
        _context = context;
        _aiService = aiService;
        TwilioClient.Init(_accountSid, _authToken);
    }

    
    public async Task ProcessIncomingMessageAsync(string incomingMessage, string phoneNumber)
    {
        // Initial syllabus text
        string syllabusText = "";

        // Check if the user exists in the database
        var user = _context.SmsUsers.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

        if (user == null)
        {
            // First-time texter - send welcome message and prompt to opt-in
            string welcomeMessage = "Hello, I am an AI-assisted messaging system that helps in your course related questions. To begin receiving messages, reply with 'BEGIN'. At any time you may reply 'QUIT' to opt-out and stop receiving messages.";

            await SendSms(phoneNumber, welcomeMessage);

            // Create a new user record and mark as first-time
            user = new SMSUser
            {
                PhoneNumber = phoneNumber,
                IsFirstTime = true,
                OptStatus = false // Default to false, user hasn't opted in yet
            };
            _context.SmsUsers.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            // If the user has opted in or replied with 'START', process the message
            if (user.OptStatus || incomingMessage.Trim().ToUpper() == "BEGIN")
            {
                var defaultClass = _context.Courses.FirstOrDefault(c => c.UsiClassIdentifier.ToUpper() == "DEFAULT");
                if (defaultClass == null)
                {
                    defaultClass = new ClassCourse
                    {
                        ClassDescription = "Default",
                        UsiClassIdentifier = "DEFAULT",
                        CourseDocuments = "Documents/Default"
                    };
                }
                if (incomingMessage.Trim().ToUpper() == "BEGIN")
                {
                    // Update opt-in status
                    user.OptStatus = true;
                    await _context.SaveChangesAsync();
                    string optInMessage = "You have successfully opted in! You will now be able to receive course-related messages.";
                    SaveSmsInteraction(phoneNumber, incomingMessage, optInMessage, defaultClass.Id);
                    await SendSms(phoneNumber, optInMessage);
                }
                else if (incomingMessage.Trim().ToUpper() == "QUIT")
                {
                    // Update opt-out status
                    user.OptStatus = false;
                    await _context.SaveChangesAsync();
                    string optInMessage = "You have successfully opted out! You will no longer be able to receive course-related messages.";
                    SaveSmsInteraction(phoneNumber, incomingMessage, optInMessage, defaultClass.Id);
                    await SendSms(phoneNumber, optInMessage);
                }

                if (incomingMessage.Trim().ToUpper() == "CHANGE") 
                {
                    user.CurrentCourse = null;
                    await _context.SaveChangesAsync();
                }
                var classCourse = new ClassCourse();
                string normalizedMessage = "";
                if (user.CurrentCourse == null)
                {
                    // Normalize and trim the incoming message
                    normalizedMessage = incomingMessage.Trim().ToUpper();
                    // Search for the class course by UsiClassIdentifier
                    classCourse = _context.Courses.FirstOrDefault(c => c.UsiClassIdentifier.ToUpper() == normalizedMessage);

                    // Update Current course to ask questions on
                    if (classCourse != null)
                    {
                        user.CurrentCourse = normalizedMessage;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    classCourse = _context.Courses.FirstOrDefault(c => c.UsiClassIdentifier.ToUpper() == user.CurrentCourse);
                }

                // Check if we need to ask for course ID
                if (string.IsNullOrEmpty(syllabusText))
                {
                    if (classCourse != null)
                    {
                        // Set syllabusText to course documents
                        syllabusText = classCourse.CourseDocuments;

                        // Proceed with AI response generation
                        var aiResponseStr = await _aiService.GenerateResponseAsync(phoneNumber, incomingMessage, syllabusText);
                        SaveSmsInteraction(phoneNumber, incomingMessage, aiResponseStr, classCourse.Id);
                        await SendSms(phoneNumber, aiResponseStr);
                    }
                    else
                    {
                        // Handle case when the course is not found
                        string errorMessage = "Please enter a valid course code. Example: ENG101. After selecting a course, type \"CHANGE\" anytime to change the course you would like to ask questions about.";
                        await SendSms(phoneNumber, errorMessage);
                    }
                }
                else
                {
                    // Once syllabusText is set, proceed with response generation
                    var aiResponseStr = await _aiService.GenerateResponseAsync(phoneNumber, incomingMessage, syllabusText);
                    SaveSmsInteraction(phoneNumber, incomingMessage, aiResponseStr, classCourse.Id);
                    await SendSms(phoneNumber, aiResponseStr);
                }
            }
            else
            {
                // If user hasn't opted in, only send the opt-in prompt
                string optInMessage = "Please reply with 'BEGIN' to begin receiving course-related messages.";
                await SendSms(phoneNumber, optInMessage);
            }
        }
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

    private void SaveSmsInteraction(string phoneNumber, string incomingMessage, string aiResponse, int ID)
    {
        // Save interaction to the database
        var smsInteraction = new SmsInteraction
        {
            PhoneNumber = phoneNumber,
            IncomingSmsMessage = incomingMessage,
            AiSmsResponse = aiResponse,
            TimeReceived = DateTime.Now,
            TimeResponded = DateTime.Now, // update this later
            CourseId = ID
        };

        _context.SmsInteractions.Add(smsInteraction);
        _context.SaveChanges();
    }
}