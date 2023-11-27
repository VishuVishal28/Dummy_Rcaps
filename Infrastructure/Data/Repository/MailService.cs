using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Data.Repository
{
    public class MailService : IMailService
    {
        private readonly MailSetting _mailSettings;
        public MailService(IOptions<MailSetting> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            // Replace these values with your Zoho Mail account information
            string subject = mailRequest.Subject;
            string body = mailRequest.Body;
            string fromEmail = "noreply@codium-tech.com";
            string toEmail = mailRequest.ToEmail;
            string smtpServer = "smtp.zoho.com";
            int smtpPort = 587;
            string username = "noreply@codium-tech.com";
            string password = "Bugs!236";

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // Create a new MailMessage
                MailMessage message = new MailMessage(fromEmail, toEmail, subject, body);

                // Set the message body format
                message.IsBodyHtml = false;

                // Create a new SmtpClient and set its properties
                SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    UseDefaultCredentials = false,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                // Send the email
                smtpClient.Send(message);

                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }   
        }
    }
}
