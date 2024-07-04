using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Models
{
    public class TemplateSendViewModel
    {
        [Required(ErrorMessage = "Please select a group.")]
        public string GroupName { get; set; }
        public List<SelectListItem> Groups { get; set; }

        [Required(ErrorMessage = "Please select a template.")]
        public string TemplateName { get; set; }
        public List<SelectListItem> Templates { get; set; }
    }
}
