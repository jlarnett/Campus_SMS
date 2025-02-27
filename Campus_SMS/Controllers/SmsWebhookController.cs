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

        public SmsWebhookController(SmsService smsService)
        {
            _smsService = smsService;
        }

        // Endpoint to handle incoming SMS messages
        [HttpPost("incoming")]
        public async Task<IActionResult> IncomingSms([FromForm] string From, [FromForm] string Body)
        {
            // Process the incoming message
            await _smsService.ProcessIncomingMessageAsync(Body, From);

            // Return the TwiML response (Twilio expects this)
            return Ok();
        }
    }
}
