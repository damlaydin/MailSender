using Microsoft.AspNetCore.Mvc;
using MailSender.Models;
using MailSender.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace MailSender.Controllers
{
    public class TemplateController : Controller
    {
        private readonly ILogger<TemplateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public TemplateController(ILogger<TemplateController> logger, ApplicationDbContext context, EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public IActionResult TemplateSend()
        {
            var groups = GetGroups();
            var templates = GetTemplates();
            var users = GetUsers();

            var viewModel = new TemplateSendViewModel
            {
                Groups = groups,
                Templates = templates,
                Users = users
            };

            return View(viewModel);
        }

        private List<SelectListItem> GetGroups()
        {
            return _context.Groups.Select(g => new SelectListItem
            {
                Value = g.GroupName,
                Text = g.GroupName
            }).ToList();
        }

        private List<SelectListItem> GetTemplates()
        {
            var templateNames = new List<string> { "TemplateEmailGroupA.html", "TemplateEmailGroupB.html" };
            return templateNames.Select(t => new SelectListItem
            {
                Value = t,
                Text = t.Replace(".html", "")
            }).ToList();
        }

        private List<SelectListItem> GetUsers()
        {
            return _context.Users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FirstName} {u.LastName}"
            }).ToList();
        }

        public IActionResult Templates()
        {
            var templates = GetTemplates();
            var viewModel = new TemplateSendViewModel
            {
                Templates = templates
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> SendTemplateEmail(TemplateSendViewModel model)
        {
            _logger.LogInformation("SendTemplateEmail POST action started.");

            string subject = $"Email for selected groups";
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", model.TemplateName);
            string bodyTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
            bodyTemplate = bodyTemplate.Replace("{{BaseUrl}}", baseUrl);

            var recipients = new List<User>();

            // Get users from selected groups
            if (model.SelectedGroupNames != null && model.SelectedGroupNames.Count > 0)
            {
                foreach (var groupName in model.SelectedGroupNames)
                {
                    var group = await _context.Groups.Include(g => g.GroupMembers).ThenInclude(m => m.User).FirstOrDefaultAsync(g => g.GroupName == groupName);
                    if (group != null && group.GroupMembers.Any())
                    {
                        recipients.AddRange(group.GroupMembers.Select(m => m.User));
                    }
                }
            }

            // Get additional users if any
            if (model.SelectedUserIds != null && model.SelectedUserIds.Count > 0)
            {
                var additionalUsers = await _context.Users.Where(u => model.SelectedUserIds.Contains(u.Id.ToString())).ToListAsync();
                recipients.AddRange(additionalUsers);
            }

            recipients = recipients.Distinct().ToList();

            foreach (var user in recipients)
            {
                string body = bodyTemplate.Replace("{{FirstName}}", user.FirstName).Replace("{{LastName}}", user.LastName);
                await SendEmail(user, subject, body, null, model.TemplateName);
            }

            TempData["SuccessMessage"] = $"Template emails sent successfully!";
            return RedirectToAction("SentMails", "Mail");
        }



        private async Task SendEmail(User user, string subject, string bodyTemplate, List<IFormFile> attachments, string templateName)
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var emailBody = bodyTemplate.Replace("{{FirstName}}", user.FirstName).Replace("{{LastName}}", user.LastName);

            // sending different image for each template
            List<string> imagePaths = new List<string>();

            if (templateName == "TemplateEmailGroupA.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\welcome.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }
            else if (templateName == "TemplateEmailGroupB.html")
            {
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\thankYou.jpg");
                imagePaths.Add(@"C:\Users\user\Desktop\MailSender\MailSender\wwwroot\images\logo.jpg");
            }

            if (attachments != null && attachments.Count > 0)
            {
                emailBody += "<br/><br/>Attachments:<br/>";
                foreach (var attachment in attachments)
                {
                    var filePath = Path.Combine("wwwroot/uploads", attachment.FileName);
                    var fileUrl = $"{baseUrl}/uploads/{attachment.FileName}";

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

            // Add to database and save changes
            _context.SentEmails.Add(sentEmail);
            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult GetTemplateContent(string templateName)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templateName);
            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound();
            }

            var templateContent = System.IO.File.ReadAllText(templatePath);
            return Json(new { content = templateContent });
        }
    }
}
