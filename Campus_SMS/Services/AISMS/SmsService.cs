using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Exceptions;
using Twilio.Types;
using Campus_SMS.Data;
using Campus_SMS.Entities;
using OpenAI.Examples;


public class SmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromPhoneNumber;
    private readonly ApplicationDbContext _context;
    private readonly AiServiceVectorStore _aiService;
    private readonly ILogger _logger;

    private DateTime interactionStartTime;
    private DateTime? timeTwilioReceivedOurRequest;


    public SmsService(ApplicationDbContext context, IConfiguration configuration, AiServiceVectorStore aiService, ILogger<SmsService> logger)
    {
        _accountSid = configuration["Twilio:AccountSID"] ?? string.Empty;
        _authToken = configuration["Twilio:AuthToken"] ?? string.Empty;
        _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"] ?? string.Empty;
        _logger = logger;
        
        _logger.LogInformation($"Initializing sms service. Initial sid = {_accountSid}, auth token = {_authToken}, fromPhoneNumber = {_fromPhoneNumber}");
        _context = context;
        _aiService = aiService;
        TwilioClient.Init(_accountSid, _authToken);
    }

    
    public async Task ProcessIncomingMessageAsync(string incomingMessage, string phoneNumber, DateTime interactionStartTime)
    {
        this.interactionStartTime = interactionStartTime;
        // Initial syllabus text
        string syllabusText = "";

        // Check if the user exists in the database
        var user = _context.SmsUsers.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

        //default class for course creation
        var defaultClass = _context.Courses.FirstOrDefault(c => c.UsiClassIdentifier.ToUpper() == "DEFAULT");
        if (defaultClass == null)
        {
            defaultClass = new ClassCourse
            {
                ClassDescription = "Default",
                UsiClassIdentifier = "DEFAULT",
                CourseDocuments = "Documents/Default"
            };
            _context.Courses.Add(defaultClass);
            await _context.SaveChangesAsync(); // Now defaultClass.Id will have a valid value
        }
        

        if (user == null)
        {
            // First-time texter - send welcome message and prompt to opt-in
            string welcomeMessage = "Hello, I am an AI-assisted messaging system that helps in your course related questions. To begin receiving messages, reply with 'BEGIN'. At any time you may reply 'QUIT' to opt-out and stop receiving messages.";
            await SendSms(phoneNumber, welcomeMessage);
            SaveSmsInteraction(phoneNumber, incomingMessage, welcomeMessage, defaultClass.Id);

            // Create a new user record and mark as first-time
            user = new SMSUser
            {
                PhoneNumber = phoneNumber,
                IsFirstTime = true,
                OptStatus = false, // Default to false, user hasn't opted in yet
                EnrolledCourses = new List<string>()
            };
            _context.SmsUsers.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {

            // If the user has opted in or replied with 'START', process the message
            if (user.OptStatus || incomingMessage.Trim().ToUpper() == "BEGIN")
            {
                if (incomingMessage.Trim().ToUpper() == "BEGIN")
                {
                    // Update opt-in status
                    user.OptStatus = true;
                    await _context.SaveChangesAsync();
                    string optInMessage = "You have successfully opted in! You will now be able to receive course-related messages. Start by adding a join key given to you by your instructor.";
                    await SendSms(phoneNumber, optInMessage);
                    SaveSmsInteraction(phoneNumber, incomingMessage, optInMessage, defaultClass.Id);
                    return;
                }
                else if (incomingMessage.Trim().ToUpper() == "QUIT")
                {
                    // Update opt-out status
                    user.OptStatus = false;
                    await _context.SaveChangesAsync();
                    string optInMessage = "You have successfully opted out! You will no longer be able to receive course-related messages.";
                    await SendSms(phoneNumber, optInMessage);
                    SaveSmsInteraction(phoneNumber, incomingMessage, optInMessage, defaultClass.Id);
                    return;
                }

                //manual course change in case of error
                if (incomingMessage.Trim() == "CHANGE!)&*") 
                {
                    user.CurrentCourse = null;
                    await _context.SaveChangesAsync();
                    return;
                }

                var classCourse = new ClassCourse();
                var classKey = new ClassCourse();
                string normalizedMessage = "";
                if (user.CurrentCourse == null)
                {
                    // Normalize and trim the incoming message
                    normalizedMessage = incomingMessage.Trim();
                    //Console.WriteLine("STUDENT SAID: " + normalizedMessage.Trim().ToUpper());

                    var availableClasses = new ClassCourse();
                    // Search for the class course by UsiClassIdentifier
                    for (int i = 0; i < user.EnrolledCourses.Count() + 1; i++)
                    {
                        var course = "";
                        if (user.EnrolledCourses.Count > 0 && i != 0) 
                        { 
                            course = user.EnrolledCourses[i-1];
                        }
                        
                        availableClasses = _context.Courses.FirstOrDefault(c => c.JoinKey.ToUpper() == course);
                        if (availableClasses != null)
                        {
                            if (availableClasses.UsiClassIdentifier == normalizedMessage)
                            {
                                classCourse = availableClasses;
                                break;
                            }
                            else
                            {
                                classCourse = null;
                            }
                        }
                        else
                        {
                            user.EnrolledCourses.Remove(course);
                            await SendSms(phoneNumber, "You have been removed from, " + course + ", due to either removal from course or course no longer exists.");
                            SaveSmsInteraction(phoneNumber, incomingMessage, "You have been removed from, " + course+", because the course no longer exists.", defaultClass.Id);
                            classCourse = null;
                        }
                    }
                    classKey = _context.Courses.FirstOrDefault(c => c.JoinKey == normalizedMessage);
                    // Update Current course to ask questions on
                    if (classCourse != null)
                    {
                        user.CurrentCourse = classCourse.JoinKey;
                        await _context.SaveChangesAsync();
                    }
                    else if (classKey != null) 
                    {
                        if (!(user.EnrolledCourses.Contains(classKey.JoinKey)))
                        {
                            user.EnrolledCourses.Add(classKey.JoinKey);
                            await _context.SaveChangesAsync();
                            SaveSmsInteraction(phoneNumber, incomingMessage, "Successfully enrolled in, " + classKey.UsiClassIdentifier + "!", defaultClass.Id);
                            await SendSms(phoneNumber, "Successfully enrolled in, " + classKey.UsiClassIdentifier + "!");
                        }
                        else
                        {
                            SaveSmsInteraction(phoneNumber, incomingMessage, "Already enrolled in, " + classKey.UsiClassIdentifier + "!", defaultClass.Id);
                            await SendSms(phoneNumber, "Already enrolled in, " + classKey.UsiClassIdentifier + ".");
                        }
                    }
                    
                }
                else
                {
                    classCourse = _context.Courses.FirstOrDefault(c => c.JoinKey.ToUpper() == user.CurrentCourse);
                    if (classCourse == null) 
                    {
                        user.CurrentCourse = null;
                        await _context.SaveChangesAsync();
                    }
                }

                

                // Check if we need to ask for course ID
                if (string.IsNullOrEmpty(syllabusText))
                {
                    if (classCourse != null)
                    {
                        // Set syllabusText to course documents
                        syllabusText = classCourse.CourseDocuments;

                        //Check if blocked from messaging course.
                        if (classCourse.BlockedNumbers != null)
                        {
                            if (classCourse.BlockedNumbers.Contains(user.PhoneNumber)) 
                            {
                                user.CurrentCourse = null;
                                await SendSms(phoneNumber, "Blocked from messaging, " + classCourse.UsiClassIdentifier + ". If you think this is a mistake, please contact your instructor.");
                                SaveSmsInteraction(phoneNumber, incomingMessage, "Blocked from messaging, " + classCourse.UsiClassIdentifier + ". If you think this is a mistake, please contact your instructor.", defaultClass.Id);
                                return;
                            }
                        }

                        // Proceed with AI response generation
                        var course = user.CurrentCourse;
                        var aiResponseStr = await _aiService.GenerateResponseAsync(phoneNumber, incomingMessage, syllabusText, course);
                        if (aiResponseStr.Trim() == "D1o0N78e")
                        {
                            user.CurrentCourse = null;
                            await _context.SaveChangesAsync();
                            await SendSms(phoneNumber, "Exiting questioning for, " + classCourse.UsiClassIdentifier + ". Please enter a new USI identifier or enter a join code. Currently enrolled class's USI identifiers include:\n" + string.Join("\n", user.EnrolledCourses.Select(course => course[..^5])));
                            SaveSmsInteraction(phoneNumber, incomingMessage, "Exiting questioning for, " + classCourse.UsiClassIdentifier + ". Please enter a new USI identifier or enter a join code. Currently enrolled class's USI identifiers include:\n" + string.Join("\n", user.EnrolledCourses.Select(course => course[..^5])), classCourse.Id);
                        }
                        else
                        {
                            await SendSms(phoneNumber, aiResponseStr);
                            SaveSmsInteraction(phoneNumber, incomingMessage, aiResponseStr, classCourse.Id);
                        }
                    }
                    else
                    {
                        // Handle case when the course is not found
                        if (user.EnrolledCourses.Count>0)
                        {
                            string unassignedMessage = ("Please enter a valid join code or start asking question on a course by using a USI identifier. Currently enrolled class's USI identifiers include:\n" + string.Join("\n", user.EnrolledCourses.Select(course => course[..^5])));
                            await SendSms(phoneNumber, unassignedMessage);
                            SaveSmsInteraction(phoneNumber, incomingMessage, unassignedMessage, defaultClass.Id);
                        }
                        else
                        {
                            string unassignedMessage = ("Please enter a valid join code or USI identifier.");
                            await SendSms(phoneNumber, unassignedMessage);
                            SaveSmsInteraction(phoneNumber, incomingMessage, unassignedMessage, defaultClass.Id);
                        }
                    }
                }
            }
            else
            {
                // If user hasn't opted in, only send the opt-in prompt
                string optInMessage = "Please reply with 'BEGIN' to begin receiving course-related messages.";
                await SendSms(phoneNumber, optInMessage);
                SaveSmsInteraction(phoneNumber, incomingMessage, optInMessage, defaultClass.Id);
            }
        }
    }

    // Send SMS
    public async Task SendSms(string toPhoneNumber, string message)
    {
        try
        {
            var messageSent = await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_fromPhoneNumber),
                body: message);

            this.timeTwilioReceivedOurRequest = messageSent.DateUpdated;
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
            TimeReceived = interactionStartTime,
            TimeResponded = timeTwilioReceivedOurRequest ?? DateTime.UtcNow, // update this later
            CourseId = ID
        };

        _context.SmsInteractions.Add(smsInteraction);
        _context.SaveChanges();
    }
}