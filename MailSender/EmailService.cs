using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Microsoft.AspNetCore.Http;
using MailKit.Security;
using MimeKit.Utils;

namespace MailSender
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(List<string> toEmails, string subject, string body, List<IFormFile> attachments = null, List<string> imagePaths = null)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SmtpUser"]));

            foreach (var email in toEmails)
            {
                message.To.Add(new MailboxAddress(email, email));
            }

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };

            if (imagePaths != null)
            {
                foreach (var imagePath in imagePaths)
                {
                    var linkedResource = new MimePart("image", "jpeg")
                    {
                        Content = new MimeContent(File.OpenRead(imagePath)),
                        ContentId = MimeUtils.GenerateMessageId(),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(imagePath)
                    };

                    bodyBuilder.LinkedResources.Add(linkedResource);
                    bodyBuilder.HtmlBody = bodyBuilder.HtmlBody.Replace($"cid:{Path.GetFileNameWithoutExtension(imagePath)}", $"cid:{linkedResource.ContentId}");
                }
            }

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.Length > 0)
                    {
                        using (var stream = new MemoryStream())
                        {
                            await attachment.CopyToAsync(stream);
                            bodyBuilder.Attachments.Add(attachment.FileName, stream.ToArray(), ContentType.Parse(attachment.ContentType));
                        }
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_configuration["EmailSettings:SmtpUser"], _configuration["EmailSettings:SmtpPass"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

    }
}

