using MrGroom_KY_SL.Data.UnitOfWork;
using MrGroom_KY_SL.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrGroom_KY_SL.Business.Services
{
    public class EmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService()
        {
            _apiKey = ConfigurationManager.AppSettings["SendGridKey"];
            _senderEmail = ConfigurationManager.AppSettings["SenderEmail"];
            _senderName = ConfigurationManager.AppSettings["SenderName"];
        }

        /// <summary>
        /// Send email and log result to EmailHistory table.
        /// bookingId is optional (nullable) — set when associated with a booking.
        /// </summary>
        public async Task SendEmailAndLogAsync(int? bookingId, string toEmail, string subject, string htmlMessage, byte[] attachmentBytes = null, string attachmentName = null)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlMessage);

            if (attachmentBytes != null && attachmentBytes.Length > 0)
            {
                msg.AddAttachment(new Attachment
                {
                    Content = Convert.ToBase64String(attachmentBytes),
                    Filename = attachmentName,
                    Type = "application/pdf",
                    Disposition = "attachment"
                });
            }

            var response = await client.SendEmailAsync(msg);

            // Read response body (if available) for logging
            string responseBody = null;
            try
            {
                if (response.Body != null)
                    responseBody = await response.Body.ReadAsStringAsync();
            }
            catch { /* ignore */ }

            // Log to DB
            using (var uow = new UnitOfWork())
            {
                var history = new EmailHistory
                {
                    BookingId = bookingId,
                    RecipientEmail = toEmail,
                    Subject = subject,
                    MessageBody = htmlMessage,
                    SentAt = DateTime.Now,
                    Status = ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300) ? "Success" : $"Failed ({(int)response.StatusCode})",
                    ErrorMessage = ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300) ? null : responseBody
                };

                uow.EmailHistoryRepository.Insert(history);
                uow.Save();
            }

            // Throw on failure so controller can show errors if needed
            if ((int)response.StatusCode >= 400)
                throw new Exception($"SendGrid returned status {(int)response.StatusCode}: {responseBody}");
        }
    }
}
