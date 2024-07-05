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

            var viewModel = new TemplateSendViewModel
            {
                Groups = groups,
                Templates = templates
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplateContent(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return BadRequest("Template name is required");
            }

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", templateName);
            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound("Template not found");
            }

            string templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
            return Content(templateContent);
        }



        [HttpPost]
        public async Task<IActionResult> SendTemplateEmail(TemplateSendViewModel model)
        {
            _logger.LogInformation("SendTemplateEmail POST action started.");

            string subject = $"Email for {model.GroupName}";
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", model.TemplateName);
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
                await SendEmail(member.User, subject, body, null, model.TemplateName);
            }

            TempData["SuccessMessage"] = $"Template emails sent successfully to {model.GroupName}!";
            return RedirectToAction("SentMails", "Mail");
        }

        [HttpGet]
        public IActionResult UpdateTemplate()
        {
            var model = new TemplateSendViewModel
            {
                Groups = GetGroups(),
                Templates = GetTemplates()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTemplate(TemplateSendViewModel model)
        {
            if (ModelState.IsValid)
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", model.TemplateName);

                // Save the template content
                await System.IO.File.WriteAllTextAsync(templatePath, model.TemplateContent);

                // Save uploaded images if any
                if (model.LogoImage != null)
                {
                    var logoPath = Path.Combine("wwwroot/images", "logo.png");
                    using (var stream = new FileStream(logoPath, FileMode.Create))
                    {
                        await model.LogoImage.CopyToAsync(stream);
                    }
                }
                if (model.BannerImage != null)
                {
                    var bannerPath = Path.Combine("wwwroot/images", "welcome.png");
                    using (var stream = new FileStream(bannerPath, FileMode.Create))
                    {
                        await model.BannerImage.CopyToAsync(stream);
                    }
                }

                ViewBag.Message = "Template updated successfully.";
            }

            model.Groups = GetGroups();
            model.Templates = GetTemplates();
            return View(model);
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

        private async Task SendEmail(User user, string subject, string bodyTemplate, List<IFormFile> attachments, string templateName)
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            var emailBody = bodyTemplate.Replace("{{FirstName}}", user.FirstName).Replace("{{LastName}}", user.LastName);


            //sending different image for each template
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
    }
}
