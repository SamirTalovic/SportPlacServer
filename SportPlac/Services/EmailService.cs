using System.Net;
using System.Net.Mail;

namespace SportPlac.Services
{
    namespace SportPlac.Services
    {
        public class EmailService
        {
            private readonly IConfiguration _config;

            public EmailService(IConfiguration config)
            {
                _config = config;
            }

            public async Task<bool> SendEmailAsync(string to, string subject, string body)
            {
                try
                {
                    var host = _config["EmailSettings:Host"];
                    var port = int.Parse(_config["EmailSettings:Port"]);
                    var email = _config["EmailSettings:Email"];
                    var password = _config["EmailSettings:Password"];

                    using var smtp = new SmtpClient(host, port)
                    {
                        Credentials = new NetworkCredential(email, password),
                        EnableSsl = true,
                        Timeout = 10000 // 🔥 10 sekundi MAX (da ne visi)
                    };

                    using var mail = new MailMessage
                    {
                        From = new MailAddress(email, "SportPlac"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mail.To.Add(to);

                    await smtp.SendMailAsync(mail);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EMAIL ERROR: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
