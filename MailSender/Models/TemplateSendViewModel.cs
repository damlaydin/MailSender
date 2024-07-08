using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace MailSender.Models
{
    public class TemplateSendViewModel
    {
        [Required(ErrorMessage = "Please select a group.")]
        public List<string> SelectedGroupNames { get; set; }
        public IEnumerable<SelectListItem> Groups { get; set; }


        [Required(ErrorMessage = "Please select a template.")]
        public string TemplateName { get; set; }
        public List<SelectListItem> Templates { get; set; }


        public List<string> SelectedUserIds { get; set; }
        public IEnumerable<SelectListItem> Users { get; set; }

        /*
                public string UpdateTemplateName { get; set; }
                public string TemplateContent { get; set; }
                public IFormFile LogoImage { get; set; }
                public IFormFile BannerImage { get; set; }
        */
    }
}
