using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace MailSender.Models
{
    public class TemplateSendViewModel
    {
        [Required(ErrorMessage = "Please select a group.")]
        public IEnumerable<SelectListItem> Groups { get; set; }


        [Required(ErrorMessage = "Please select a template.")]
        public string TemplateName { get; set; }
        public List<SelectListItem> Templates { get; set; }

        public IEnumerable<SelectListItem> Users { get; set; }

        public Dictionary<string, List<string>> TemplateVariables { get; set; }

        public List<string> SelectedGroupNames { get; set; } = new List<string>();
        public List<string> SelectedUserIds { get; set; } = new List<string>();
        public string TemplateContent { get; set; }


    }
}
