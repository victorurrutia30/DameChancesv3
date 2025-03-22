using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace DameChanceSV2.Services
{

    public interface IEmailService
    {
        void SendEmail(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            // Lee la sección MailSettings
            var mailSettings = _configuration.GetSection("MailSettings");

            // De cada clave, obtén el valor
            var fromEmail = mailSettings.GetValue<string>("FromEmail");   // Debe coincidir con "FromEmail"
            var displayName = mailSettings.GetValue<string>("DisplayName");
            var username = mailSettings.GetValue<string>("Username");
            var password = mailSettings.GetValue<string>("Password");
            var host = mailSettings.GetValue<string>("Host");
            var port = mailSettings.GetValue<int>("Port");

            // Verifica que fromEmail NO sea null
            // (puedes hacer un debug o poner un breakpoint para revisar sus valores)
            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(username, password);

                // Construye el remitente con el correo real
                var fromAddress = new MailAddress(fromEmail, displayName);
                var toAddress = new MailAddress(toEmail);

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    client.Send(message);
                }
            }
        }
    }

}
