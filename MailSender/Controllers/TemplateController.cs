using MailSender.Data;
using MailSender.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRChat.Hubs;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MailSender.Controllers
{
    public class TemplateController : Controller
    {
        private readonly ILogger<TemplateController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IHubContext<ChatHub> _hubContext;

        public TemplateController(ILogger<TemplateController> logger, ApplicationDbContext context, EmailService emailService, IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        public IActionResult TemplateSend()
        {
            var viewModel = new TemplateSendViewModel
            {
                Groups = GetGroups(),
                Templates = GetTemplates(),
                Users = GetUsers()
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
            return _context.Templates.Select(t => new SelectListItem
            {
                Value = t.Name,
                Text = t.Name
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
            var viewModel = new TemplateSendViewModel
            {
                Groups = GetGroups(),
                Templates = GetTemplates(),
                Users = GetUsers()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendTemplateEmail(TemplateSendViewModel model)
        {
            var template = await _context.Templates.FirstOrDefaultAsync(t => t.Name == model.TemplateName);

            if (template == null)
            {
                TempData["ErrorMessage"] = $"Template {model.TemplateName} not found.";
                return RedirectToAction("TemplateSend");
            }

            string subject = template.Subject;
            string bodyTemplate = template.Body;

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

            var imageCids = ExtractCidsFromHtml(bodyTemplate);

            foreach (var user in recipients)
            {
                string body = bodyTemplate.Replace("{{FirstName}}", user.FirstName).Replace("{{LastName}}", user.LastName);
                await SendEmail(user, subject, body, imageCids);
            }

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "System", "Email sent successfully.");

            TempData["SuccessMessage"] = $"Template emails sent successfully!";
            return RedirectToAction("Index", "Home");
        }

        private List<string> ExtractCidsFromHtml(string html)
        {
            var cids = new List<string>();
            var cidRegex = new Regex(@"cid:(\w+)", RegexOptions.IgnoreCase);
            var matches = cidRegex.Matches(html);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    cids.Add(match.Groups[1].Value);
                }
            }
            return cids;
        }

        private async Task SendEmail(User user, string subject, string bodyTemplate, List<string> imageCids)
        {
            var emailBody = bodyTemplate.Replace("{{FirstName}}", user.FirstName).Replace("{{LastName}}", user.LastName);

            var imagePaths = new List<string>();
            foreach (var cid in imageCids)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", $"{cid}.jpg");
                if (System.IO.File.Exists(filePath))
                {
                    imagePaths.Add(filePath);
                }
            }

            await _emailService.SendEmailAsync(new List<string> { user.Email }, subject, emailBody, imagePaths);

            var sentEmail = new SentEmail
            {
                Subject = subject,
                Body = emailBody,
                SentDate = DateTime.Now,
                SentTo = user.Email
            };

            _context.SentEmails.Add(sentEmail);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> UpdateTemplate(string templateName, string templateContent)
        {
            var template = await _context.Templates.FirstOrDefaultAsync(t => t.Name == templateName);
            if (template == null)
            {
                return NotFound();
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            templateContent = ReplaceUrlsWithCid(templateContent, baseUrl);

            template.Body = templateContent;
            _context.Templates.Update(template);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private string ReplaceUrlsWithCid(string content, string baseUrl)
        {
            var regex = new Regex(@"<img[^>]+src=""([^""]+)""[^>]*>", RegexOptions.IgnoreCase);
            return regex.Replace(content, match =>
            {
                var src = match.Groups[1].Value;
                if (src.StartsWith(baseUrl + "/images/"))
                {
                    var fileName = src.Substring((baseUrl + "/images/").Length);
                    var cid = Path.GetFileNameWithoutExtension(fileName);
                    return match.Value.Replace(src, $"cid:{cid}");
                }
                return match.Value;
            });
        }


        [HttpGet]
        public IActionResult GetTemplateContent(string templateName)
        {
            var template = _context.Templates.FirstOrDefault(t => t.Name == templateName);
            if (template == null)
            {
                return NotFound();
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var templateContent = ReplaceCidWithUrls(template.Body, baseUrl);

            return Json(new { content = templateContent });
        }

        private string ReplaceCidWithUrls(string content, string baseUrl)
        {
            return Regex.Replace(content, @"cid:(\w+)", $"{baseUrl}/images/$1.jpg");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { location = string.Empty });
            }

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var fileUrl = $"{baseUrl}/images/{fileName}";

            return Ok(new { location = fileUrl });
        }
    }
}
