using Microsoft.AspNetCore.Mvc;
using Twilio.TwiML;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Threading.Tasks;

namespace Campus_SMS.Controllers
{
    [Route("sms")]
    [ApiController]
    public class SmsWebhookController : ControllerBase
    {
        private readonly SmsService _smsService;
        private readonly ILogger _logger;

        public SmsWebhookController(SmsService smsService, ILogger<SmsWebhookController> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        // Endpoint to handle incoming SMS messages
        [HttpPost("incoming")]
        public async Task<IActionResult> IncomingSms([FromForm] string From, [FromForm] string Body)
        {
            var interactionStartTime = DateTime.UtcNow;
            try
            {
                // Process the incoming message
                await _smsService.ProcessIncomingMessageAsync(Body, From, interactionStartTime);

                // Return the TwiML response (Twilio expects this)
                return Ok();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
