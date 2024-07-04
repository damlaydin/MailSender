using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MailSender.Data
{
    [Table("SentEmail")]
    public class SentEmail
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
        public string SentTo { get; set; }
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

    }
}
