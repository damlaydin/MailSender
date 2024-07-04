using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MailSender.Data;

namespace MailSender.Models
{
    public class SendMailViewModel
    {
        [Required(ErrorMessage = "Sender email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string SenderEmail { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Body is required.")]
        public string Body { get; set; }

        public List<IFormFile> Attachments { get; set; } = new List<IFormFile>();

        [Required(ErrorMessage = "Please select a group.")]
        public int SelectedGroupId { get; set; }

        public List<SelectListItem> Groups { get; set; } = new List<SelectListItem>();

    }
}
