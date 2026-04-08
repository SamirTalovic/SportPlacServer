using Microsoft.AspNetCore.Mvc;
using SportPlac.Services.SportPlac.Services;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/test-email")]
    public class EmailTestController : ControllerBase
    {
        private readonly EmailService _emailService;

        public EmailTestController(EmailService emailService)
        {
            _emailService = emailService;
        }

        // POST api/test-email
        [HttpPost]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailDto dto)
        {
            if (string.IsNullOrEmpty(dto.To))
                return BadRequest("Email is required");

            await _emailService.SendEmailAsync(
                dto.To,
                "Test email from SportPlac 🚀",
                $@"
                <h2>Test uspešan ✅</h2>
                <p>Ovo je test email poslat sa backenda.</p>
                <p><b>Poruka:</b> {dto.Message}</p>
                <br/>
                <small>SportPlac system</small>
                "
            );

            return Ok("Email sent successfully");
        }
    }

    public class TestEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Message { get; set; } = "Hello from backend!";
    }
}
