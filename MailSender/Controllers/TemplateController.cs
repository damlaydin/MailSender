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
        public async Task<IActionResult> SendTemplateEmail(string templateName)
        {
            var template = await _context.Templates
                .Include(t => t.TemplateGroups)
                .ThenInclude(tg => tg.Group)
                .ThenInclude(g => g.GroupMembers)
                .ThenInclude(gm => gm.User)
                .Include(t => t.TemplateUsers)
                .ThenInclude(tu => tu.User)
                .FirstOrDefaultAsync(t => t.Name == templateName);

            if (template == null)
            {
                TempData["ErrorMessage"] = $"Template {templateName} not found.";
                return RedirectToAction("Index", "Home");
            }

            string subject = template.Subject;
            string bodyTemplate = template.Body;

            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
            bodyTemplate = ReplaceUrlsWithCid(bodyTemplate, baseUrl);

            var recipients = new List<User>();

            if (template.TemplateGroups != null && template.TemplateGroups.Count > 0)
            {
                foreach (var templateGroup in template.TemplateGroups)
                {
                    if (templateGroup.Group.GroupMembers != null && templateGroup.Group.GroupMembers.Count > 0)

                        recipients.AddRange(templateGroup.Group.GroupMembers.Select(m => m.User));

                }
            }

            // Get users from associated template users
            if (template.TemplateUsers != null && template.TemplateUsers.Count > 0)
            {
                recipients.AddRange(template.TemplateUsers.Select(tu => tu.User));
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

        [HttpPost]
        public async Task<IActionResult> UpdateTemplate(TemplateSendViewModel model)
        {
            var template = await _context.Templates
                .Include(t => t.TemplateGroups)
                .Include(t => t.TemplateUsers)
                .FirstOrDefaultAsync(t => t.Name == model.TemplateName);

            if (template == null)
            {
                return NotFound();
            }

            // Compare existing groups with selected groups
            var existingGroupIds = template.TemplateGroups.Select(tg => tg.GroupId).ToList();
            var selectedGroupIds = new List<int>();

            if (model.SelectedGroupNames != null && model.SelectedGroupNames.Count > 0)
            {
                foreach (var groupName in model.SelectedGroupNames)
                {
                    var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == groupName);
                    if (group != null)
                    {
                        selectedGroupIds.Add(group.Id);
                    }
                }
            }

            var differentGroups = !existingGroupIds.SequenceEqual(selectedGroupIds);

            // Compare existing users with selected users
            var existingUserIds = template.TemplateUsers.Select(tu => tu.UserId).ToList();
            var selectedUserIds = new List<int>();

            if (model.SelectedUserIds != null && model.SelectedUserIds.Count > 0)
            {
                foreach (var userId in model.SelectedUserIds)
                {
                    if (int.TryParse(userId, out int uid))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == uid);
                        if (user != null)
                        {
                            selectedUserIds.Add(user.Id);
                        }
                    }
                }
            }

            var differentUsers = !existingUserIds.SequenceEqual(selectedUserIds);

            // Clear existing associations if they are different
            if (differentGroups)
            {
                _context.TemplateGroups.RemoveRange(template.TemplateGroups);

                foreach (var groupId in selectedGroupIds)
                {
                    _context.TemplateGroups.Add(new TemplateGroup
                    {
                        TemplateId = template.Id,
                        GroupId = groupId
                    });
                }
            }

            if (differentUsers)
            {
                _context.TemplateUsers.RemoveRange(template.TemplateUsers);

                foreach (var userId in selectedUserIds)
                {
                    _context.TemplateUsers.Add(new TemplateUser
                    {
                        TemplateId = template.Id,
                        UserId = userId
                    });
                }
            }

            // Convert image sources to CID
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
            var updatedTemplateContent = ReplaceUrlsWithCid(model.TemplateContent, baseUrl);

            // Update template body
            template.Body = updatedTemplateContent;
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

        [HttpGet]
        public async Task<IActionResult> GetTemplateContentWithVariables(string templateName)
        {
            try
            {
                var template = await _context.Templates
                                             .Include(t => t.TemplateVariables)
                                             .ThenInclude(tv => tv.Variable)
                                             .FirstOrDefaultAsync(t => t.Name == templateName);

                if (template == null)
                {
                    return NotFound(new { message = $"Template with name {templateName} not found." });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var templateContent = ReplaceCidWithUrls(template.Body, baseUrl);

                // Fetch variable names
                var variableNames = template.TemplateVariables.Select(tv => tv.Variable.Var_name).ToList();

                var varDesc = template.TemplateVariables.Select(tv => tv.Variable.Description).ToList();

                return Json(new { content = templateContent, variableNames = variableNames, variableDescriptions = varDesc });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the template content.");
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }


    }
}
