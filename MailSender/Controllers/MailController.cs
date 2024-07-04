using Microsoft.AspNetCore.Mvc;
using MailSender.Models;
using MailSender.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using MimeKit;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MailSender.Controllers
{
    public class MailController : Controller
    {
        private readonly ILogger<MailController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public MailController(ILogger<MailController> logger, ApplicationDbContext context, EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public IActionResult SendMail()
        {
            var groups = _context.Groups.Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.GroupName
            }).ToList();

            var viewModel = new SendMailViewModel
            {
                Groups = groups
            };

            return View(viewModel);
        }

        [HttpPost]
        private async Task SendEmail(User user, string subject, string bodyTemplate, List<IFormFile> attachments, string templateName)
        {
            // Get the current HTTP request
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

            // Replace body template with userd actual data
            var emailBody = bodyTemplate
                .Replace("{{FirstName}}", user.FirstName)
                .Replace("{{LastName}}", user.LastName);


            //sending different image for each template
            List<string> imagePaths = new List<string>();

            if (templateName == "TemplateEmailGroupA.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\thankYou.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }
            else if (templateName == "TemplateEmailGroupB.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\welcome.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }


            // Add attachment links to the email body
            if (attachments != null && attachments.Count > 0)
            {
                emailBody += "<br/><br/>Attachments:<br/>";
                foreach (var attachment in attachments)
                {
                    // Construct the file path and URL for each attachment
                    var filePath = Path.Combine("wwwroot/uploads", attachment.FileName);
                    var fileUrl = $"{baseUrl}/uploads/{attachment.FileName}";

                    // Save the attachment file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileStream);
                    }

                    emailBody += $"<a href=\"{fileUrl}\" target=\"_blank\">{attachment.FileName}</a><br/>";
                }
            }

            await _emailService.SendEmailAsync(new List<string> { user.Email }, subject, emailBody, attachments, imagePaths);

            var sentEmail = new SentEmail
            {
                Subject = subject,
                Body = emailBody,
                SentDate = DateTime.Now,
                SentTo = user.Email
            };

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await attachment.CopyToAsync(memoryStream);
                            sentEmail.Attachments.Add(new EmailAttachment
                            {
                                FileName = attachment.FileName,
                                ContentType = attachment.ContentType,
                                Data = memoryStream.ToArray()
                            });
                        }
                    }
                }
            }

            // Add to database and save changes
            _context.SentEmails.Add(sentEmail);
            await _context.SaveChangesAsync();
        }


        // to display a list of sent emails
        public async Task<IActionResult> SentMails()
        {
            var sentMails = await _context.SentEmails.Include(e => e.Attachments).ToListAsync();
            return View(sentMails);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var email = await _context.SentEmails.Include(e => e.Attachments).FirstOrDefaultAsync(e => e.Id == id);
            if (email == null)
            {
                return NotFound();
            }

            _context.SentEmails.Remove(email);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email deleted successfully!";
            return RedirectToAction(nameof(SentMails));
        }

        public async Task<IActionResult> GetEmail(int id)
        {
           // get the email with the specified ID
            var email = await _context.SentEmails.Include(e => e.Attachments).FirstOrDefaultAsync(e => e.Id == id);
            if (email == null)
            {
                return NotFound();
            }

            // Return the email detail with JSON 
            return Json(new { id = email.Id, subject = email.Subject, body = email.Body });
        }







        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            // Check if the uploaded file is valid
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            // Create the uploads directory
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            var filePath = Path.Combine(uploads, file.FileName);

            // Save the uploaded file to  server
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var fileUrl = $"{baseUrl}/uploads/{file.FileName}";

            _logger.LogInformation($"File URL: {fileUrl}");

            return Json(new { location = fileUrl });
        }

        /*
        public IActionResult TemplateSend()
        {
            // Get the list of groups from the database
            // create SelectListItems for the dropdown
            var groups = _context.Groups.Select(g => new SelectListItem
            {
                Value = g.GroupName,
                Text = g.GroupName
            }).ToList();


            //List of email templates
            var templates = new List<SelectListItem>
            {
                new SelectListItem { Value = "TemplateEmailGroupA.html", Text = "Group A Template" },
                new SelectListItem { Value = "TemplateEmailGroupB.html", Text = "Group B Template" }
            };

            var viewModel = new TemplateSendViewModel
            {
                Groups = groups,
                Templates = templates
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendTemplateEmail(TemplateSendViewModel model)
        {
            _logger.LogInformation("SendTemplateEmail POST action started.");

            // the subject of the email
            string subject = $"Email for {model.GroupName}";

            // Define the path to the email template
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", model.TemplateName);

            //Read the email 
            string bodyTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
            bodyTemplate = bodyTemplate.Replace("{{BaseUrl}}", baseUrl);

            var group = await _context.Groups.Include(g => g.GroupMembers).ThenInclude(m => m.User).FirstOrDefaultAsync(g => g.GroupName == model.GroupName);
            if (group == null || !group.GroupMembers.Any())
            {
                _logger.LogWarning($"No users found in {model.GroupName}.");
                TempData["ErrorMessage"] = $"No users found in {model.GroupName}.";
                return RedirectToAction("TemplateSend");
            }

            foreach (var member in group.GroupMembers)
            {
                string body = bodyTemplate.Replace("{{FirstName}}", member.User.FirstName).Replace("{{LastName}}", member.User.LastName);
                // Send the email
                await SendEmail(member.User, subject, body, null, model.TemplateName);
            }

            TempData["SuccessMessage"] = $"Template emails sent successfully to {model.GroupName}!";
            return RedirectToAction("SentMails");
        }


        //BU DEĞİŞECEK
        [HttpPost]
        public async Task<IActionResult> UpdateEmail(int EmailId, string Subject, string Body, List<IFormFile> Attachments, string TemplateName)
        {
            var email = await _context.SentEmails.Include(e => e.Attachments).FirstOrDefaultAsync(e => e.Id == EmailId);
            if (email == null)
            {
                return NotFound();
            }

            email.Subject = Subject;
            email.Body = Body;

            // Process attachments and add links to the body
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

            if (Attachments != null && Attachments.Count > 0)
            {
                Body += "<br/><br/>Attachments:<br/>";
                foreach (var attachment in Attachments)
                {
                    var filePath = Path.Combine("wwwroot/uploads", attachment.FileName);
                    var fileUrl = $"{baseUrl}/uploads/{attachment.FileName}";

                    // Save the attachment file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(fileStream);
                    }

                    Body += $"<a href=\"{fileUrl}\" target=\"_blank\">{attachment.FileName}</a><br/>";

                    // Add attachment to the email
                    email.Attachments.Add(new EmailAttachment
                    {
                        FileName = attachment.FileName,
                        ContentType = attachment.ContentType,
                        Data = System.IO.File.ReadAllBytes(filePath)
                    });
                }
            }

            var user = new User { Email = email.SentTo, FirstName = "", LastName = "" };

            //sending different image for each template
            List<string> imagePaths = new List<string>();

            if (TemplateName == "TemplateEmailGroupA.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\thankYou.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }
            else if (TemplateName == "TemplateEmailGroupB.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\welcome.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }


            await SendEmail(user, email.Subject, email.Body, Attachments, TemplateName);

            // Update email body with images
            foreach (var imagePath in imagePaths)
            {
                var imageFileName = Path.GetFileName(imagePath);
                var imageFileUrl = $"{baseUrl}/images/{imageFileName}";
                email.Body = email.Body.Replace(imageFileName, imageFileUrl);
            }

            _context.SentEmails.Update(email);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email updated and sent successfully!";
            return RedirectToAction(nameof(SentMails));
        }
        
         */

    }
}