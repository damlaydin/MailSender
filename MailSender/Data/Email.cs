
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Data
{
    [Table("Emails")]
    public class Email
    {
        public int Id { get; set; }

        [Display(Name = "Sender Email")]
        public string SenderEmail { get; set; }

        [Display(Name = "Recipient Email")]
        public string RecipientEmail { get; set; }

        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Display(Name = "Body")]
        public string Body { get; set; }
    }
}
